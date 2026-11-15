using System;
using System.Collections.Generic;

namespace KingdomRightClickFix
{
    /// <summary>Optional registration API allowing external mods to claim custom right-click UI controls.</summary>
    public static class RightClickFixApi
    {
        private sealed class Registration : IDisposable
        {
            private readonly Func<bool> predicate;
            private bool disposed;
            internal string OwnerId { get; }

            internal Registration(string ownerId, Func<bool> predicate)
            {
                OwnerId = ownerId ?? string.Empty;
                this.predicate = predicate;
            }

            internal bool Matches() => !disposed && predicate();

            public void Dispose()
            {
                if (disposed)
                    return;
                disposed = true;
                Unregister(this);
            }
        }

        private static readonly object Gate = new object();
        private static readonly List<Registration> Handlers = new List<Registration>();

        /// <summary>Register a custom right-click control predicate scoped to <paramref name="ownerId"/>.</summary>
        public static IDisposable RegisterConsumer(string ownerId, Func<bool> isPointerOverConsumer)
        {
            if (isPointerOverConsumer == null)
                throw new ArgumentNullException(nameof(isPointerOverConsumer));
            var entry = new Registration(ownerId, isPointerOverConsumer);
            lock (Gate)
                Handlers.Add(entry);
            return entry;
        }

        /// <summary>Register a disposable custom right-click control predicate.</summary>
        public static IDisposable RegisterConsumer(Func<bool> isPointerOverConsumer)
            => RegisterConsumer(string.Empty, isPointerOverConsumer);

        /// <summary>Remove all custom right-click control predicates registered by <paramref name="ownerId"/>.</summary>
        public static int UnregisterOwner(string ownerId)
        {
            if (string.IsNullOrEmpty(ownerId))
                return 0;
            lock (Gate)
                return Handlers.RemoveAll(item => string.Equals(item.OwnerId, ownerId, StringComparison.Ordinal));
        }

        internal static bool AnyConsumerClaimsPointer()
        {
            Registration[] snapshot;
            lock (Gate)
            {
                if (Handlers.Count == 0)
                    return false;
                snapshot = Handlers.ToArray();
            }

            foreach (var entry in snapshot)
            {
                try
                {
                    if (entry.Matches())
                        return true;
                }
                catch (Exception ex)
                {
                    RightClickFixPlugin.LogError("Custom right-click consumer predicate failed: " + ex);
                }
            }

            return false;
        }

        internal static void ClearAll()
        {
            lock (Gate)
                Handlers.Clear();
        }

        private static void Unregister(Registration entry)
        {
            lock (Gate)
                Handlers.Remove(entry);
        }
    }
}

