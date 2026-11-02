// A standalone regression harness for the actual subscriber source and ROS# messages.
// Minimal Unity doubles test routing/queue behavior; they do not replace Unity play-mode tests.
#if STANDALONE_BOTTLE_TEST
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RosSharp.RosBridgeClient.MessageTypes.PlaceEstimation;
using RosSharp.RosBridgeClient.MessageTypes.Geometry;
using UnityEngine;
using Numerics = System.Numerics;

internal static class TestThread
{
    public static readonly int MainId = Thread.CurrentThread.ManagedThreadId;
    public static void Check()
    {
        if (Thread.CurrentThread.ManagedThreadId != MainId)
            throw new Exception("Unity object API was accessed from a callback thread.");
    }
}

namespace UnityEngine
{
    public class Object
    {
        public bool Destroyed;
        public string name { get { TestThread.Check(); return "TestObject"; } }
        public static bool operator ==(Object a, Object b)
        {
            TestThread.Check();
            bool aNull = ReferenceEquals(a, null) || a.Destroyed;
            bool bNull = ReferenceEquals(b, null) || b.Destroyed;
            return aNull || bNull ? aNull == bNull : ReferenceEquals(a, b);
        }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => base.GetHashCode();
    }

    public class MonoBehaviour : Object { }
    public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string text) { } }
    public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string text) { } }
    public sealed class SerializeField : Attribute { }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }

    public class Transform : Object
    {
        private Vector3 currentPosition;
        public int PositionWrites;
        public Numerics.Quaternion rotation = Numerics.Quaternion.Identity;
        public Vector3 localScale = new Vector3(1, 1, 1);
        public Vector3 position
        {
            get { TestThread.Check(); return currentPosition; }
            set { TestThread.Check(); PositionWrites++; currentPosition = value; }
        }
        public Vector3 TransformPoint(Vector3 local)
        {
            TestThread.Check();
            var p = Numerics.Vector3.Transform(
                new Numerics.Vector3(local.x * localScale.x, local.y * localScale.y, local.z * localScale.z),
                rotation);
            return new Vector3(p.X + position.x, p.Y + position.y, p.Z + position.z);
        }
    }

    public class GameObject : Object
    {
        private readonly Transform targetTransform = new Transform();
        public Transform transform { get { TestThread.Check(); return targetTransform; } }
    }

    public static class Debug
    {
        public static readonly List<string> Warnings = new List<string>();
        public static readonly List<string> Errors = new List<string>();
        public static void Log(object text) { TestThread.Check(); }
        public static void LogWarning(object text) { TestThread.Check(); Warnings.Add(text.ToString()); }
        public static void LogError(object text) { TestThread.Check(); Errors.Add(text.ToString()); }
    }
}

namespace RosSharp.RosBridgeClient
{
    public abstract class UnitySubscriber<T> : MonoBehaviour where T : Message
    {
        public string Topic;
        public int SubscribeCalls;
        protected virtual void Start() { SubscribeCalls++; }
        protected abstract void ReceiveMessage(T message);
    }
}

internal sealed class ProbeSubscriber : CorrectedBottlePositionSubscriber
{
    private static readonly MethodInfo UpdateMethod = typeof(CorrectedBottlePositionSubscriber)
        .GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
    public void Deliver(BottleDistribution message) { ReceiveMessage(message); }
    public void Tick() { UpdateMethod.Invoke(this, null); }
    public void StartForTest() { Start(); }
}

internal static class Program
{
    private static int passed;
    private static void Require(bool value, string text)
    {
        if (!value) throw new Exception(text);
    }
    private static void Position(GameObject bottle, float x, float y, float z)
    {
        var actual = bottle.transform.position;
        Require(Math.Abs(actual.x - x) < 0.0001f && Math.Abs(actual.y - y) < 0.0001f &&
            Math.Abs(actual.z - z) < 0.0001f, "Unexpected target position.");
    }
    private static BottleDistribution Result(int id, double x = 1, double y = 2, double z = 3)
    {
        var message = new BottleDistribution { bottle_id = id, position = new Point(x, y, z) };
        message.header.seq = 999; // Deliberately unrelated to bottle_id.
        return message;
    }
    private static void Test(string name, Action action)
    {
        action();
        passed++;
        Console.WriteLine("PASS " + name);
    }
    private static void Main()
    {
        TestThread.Check();
        Test("ROS# message type, JSON fields, covariance length", () =>
        {
            Require(BottleDistribution.RosMessageName == "place_estimation/BottleDistribution", "Wrong ROS type.");
            Require(new BottleDistribution().covariance.Length == 9, "Wrong fixed covariance size.");
            var decoded = JsonSerializer.Deserialize<BottleDistribution>(
                "{\"header\":{\"seq\":12},\"bottle_id\":7,\"position\":{\"x\":1,\"y\":2,\"z\":3},\"covariance\":[1,0,0,0,1,0,0,0,1]}");
            Require(decoded.bottle_id == 7 && decoded.header.seq == 12 && decoded.position.z == 3,
                "Message fields were not decoded correctly.");
        });

        Test("Legacy/empty topics migrate with only one subscription", () =>
        {
            foreach (var oldTopic in new[] { "", "/P_place", "/P_place_with_id" })
            {
                var probe = new ProbeSubscriber { Topic = oldTopic };
                probe.StartForTest();
                Require(probe.Topic == "/P_place_with_id" && probe.SubscribeCalls == 1, "Wrong subscription.");
            }
        });

        Test("Two IDs: result selects ID 7 even when ID 12 was registered last", () =>
        {
            var probe = new ProbeSubscriber();
            var a = new GameObject(); var b = new GameObject(); var origin = new Transform();
            Require(probe.ArmForBottle(7, a, origin) && probe.ArmForBottle(12, b, origin), "Registration failed.");
            Task.Run(() => probe.Deliver(Result(7))).GetAwaiter().GetResult();
            Position(a, 0, 0, 0); // Callback must only queue data.
            probe.Tick();
            Position(a, -1, -3, 2); Position(b, 0, 0, 0);
        });

        Test("7 -> 12 -> 7 registration retains both IDs and queues all results", () =>
        {
            var probe = new ProbeSubscriber();
            var a = new GameObject(); var b = new GameObject(); var origin = new Transform();
            Require(probe.ArmForBottle(7, a, origin) && probe.ArmForBottle(12, b, origin) &&
                probe.ArmForBottle(7, a, origin), "Same-bottle registration must succeed.");
            probe.Deliver(Result(7, 1, 2, 3));
            probe.Deliver(Result(12, 4, 5, 6));
            probe.Deliver(Result(7, 7, 8, 9));
            probe.Tick();
            Position(a, -7, -9, 8); Position(b, -4, -6, 5);
            Require(a.transform.PositionWrites == 2 && b.transform.PositionWrites == 1, "Result overwritten or lost.");
        });

        Test("Unregistered and uninitialized IDs are ignored and logged", () =>
        {
            var probe = new ProbeSubscriber(); var bottle = new GameObject(); var origin = new Transform();
            Require(!probe.ArmForBottle(-1, bottle, origin), "ID -1 must be rejected.");
            Require(probe.ArmForBottle(0, bottle, origin), "ID 0 must remain valid.");
            var before = Debug.Warnings.Count;
            probe.Deliver(Result(99)); probe.Deliver(Result(-1)); probe.Tick();
            Position(bottle, 0, 0, 0);
            Require(Debug.Warnings.Count == before + 2, "Ignored IDs must be logged.");
        });

        Test("Duplicate ID cannot replace another bottle", () =>
        {
            var probe = new ProbeSubscriber(); var a = new GameObject(); var b = new GameObject();
            var origin = new Transform(); var before = Debug.Errors.Count;
            Require(probe.ArmForBottle(7, a, origin), "Registration failed.");
            Require(!probe.ArmForBottle(7, b, origin), "Duplicate ID must be rejected.");
            Require(Debug.Errors.Count == before + 1, "Duplicate must be reported.");
            probe.Deliver(Result(7)); probe.Tick();
            Position(a, -1, -3, 2); Position(b, 0, 0, 0);
        });

        Test("Destroyed bottle or ObjectList never redirects a result", () =>
        {
            var probe = new ProbeSubscriber(); var a = new GameObject(); var b = new GameObject();
            var origin = new Transform(); var before = Debug.Warnings.Count;
            probe.ArmForBottle(7, a, origin); probe.ArmForBottle(12, b, origin);
            a.Destroyed = true; probe.Deliver(Result(7)); probe.Tick();
            Require(a.transform.PositionWrites == 0 && b.transform.PositionWrites == 0, "Destroyed target applied.");
            origin.Destroyed = true; probe.Deliver(Result(12)); probe.Tick();
            Require(b.transform.PositionWrites == 0 && Debug.Warnings.Count == before + 2, "Destroyed origin applied.");
        });

        Test("Existing axis conversion, ObjectList transform, rotation preservation", () =>
        {
            var probe = new ProbeSubscriber(); var bottle = new GameObject();
            var origin = new Transform
            {
                position = new Vector3(10, 20, 30), localScale = new Vector3(2, 3, 4),
                rotation = Numerics.Quaternion.CreateFromAxisAngle(Numerics.Vector3.UnitZ, (float)Math.PI / 2)
            };
            var rotation = Numerics.Quaternion.CreateFromAxisAngle(Numerics.Vector3.UnitX, 0.4f);
            bottle.transform.rotation = rotation;
            probe.ArmForBottle(7, bottle, origin); probe.Deliver(Result(7)); probe.Tick();
            Position(bottle, 19, 18, 38);
            Require(bottle.transform.rotation == rotation, "Bottle rotation was overwritten.");
        });

        Test("Concurrent callbacks preserve all 400 results without Unity API calls", () =>
        {
            var probe = new ProbeSubscriber(); var a = new GameObject(); var b = new GameObject();
            probe.ArmForBottle(7, a, new Transform()); probe.ArmForBottle(12, b, new Transform());
            Parallel.Invoke(
                () => { for (int i = 0; i < 200; i++) probe.Deliver(Result(7, i, 2, 3)); },
                () => { for (int i = 0; i < 200; i++) probe.Deliver(Result(12, i, 4, 5)); });
            Require(a.transform.PositionWrites == 0 && b.transform.PositionWrites == 0, "Callback moved a bottle.");
            probe.Tick();
            Require(a.transform.PositionWrites == 200 && b.transform.PositionWrites == 200, "Queued results lost.");
            Position(a, -199, -3, 2); Position(b, -199, -5, 4);
        });

        Test("Queued values are snapshots; cancellation clears targets and queue", () =>
        {
            var probe = new ProbeSubscriber(); var bottle = new GameObject();
            probe.ArmForBottle(7, bottle, new Transform());
            var message = Result(7); probe.Deliver(message);
            message.bottle_id = 99; message.position.x = 999;
            probe.Tick(); Position(bottle, -1, -3, 2);
            probe.Deliver(Result(7, 10)); probe.CancelPendingResult(); probe.Tick();
            probe.Deliver(Result(7, 20)); probe.Tick(); Position(bottle, -1, -3, 2);
        });
        Console.WriteLine("Passed " + passed + " standalone checks (Unity/ROS integration not exercised).");
    }
}
#endif
