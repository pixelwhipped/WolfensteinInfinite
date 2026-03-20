namespace WolfensteinInfinite.Utilities
{
    public static class NodeHelpers
    {
        public static bool TryAdd<TKey, TValue>(this HashSet<NodeInterconnect<TKey, TValue>> nodes, NodeInterconnect<TKey, TValue> value) where TKey : notnull where TValue : IConnection<TKey, TValue>
        {
            if (nodes.Any(p => p.Connection.Equals(value.Connection))) return false;
            nodes.Add(value);
            return true;
        }
        public static bool TryGetValue<TKey, TValue>(this NodeConnection<TKey, TValue>[] nodes, TKey key, out NodeConnection<TKey, TValue>? value) where TKey : notnull where TValue : IConnection<TKey, TValue>
        {
            var i = Array.FindIndex(nodes, p => p.Key.Equals(key));
            value = i >= 0 ? nodes[i] : null;
            return value != null;
        }
        public static bool TryGetValue<TKey, TValue>(this HashSet<NodeInterconnect<TKey, TValue>> nodes, TKey connection, out NodeInterconnect<TKey, TValue>? value) where TKey : notnull where TValue : IConnection<TKey, TValue>
        {
            foreach (var n in nodes)
            {
                if (n.Connection.Equals(connection))
                {
                    value = n;
                    return true;
                }
            }
            value = null;
            return false;
        }
        public static bool ContainsAnyDuplicates<T>(this IEnumerable<T> collection)
        {
            HashSet<T> seenElements = [];
            return collection.Any(item => !seenElements.Add(item));
        }
    }
    public interface IConnection<TKey, TValue> where TKey : notnull where TValue : IConnection<TKey, TValue>
    {
        public NodeConnection<TKey, TValue>[] Connections { get; init; }
        public int Count => Connections.Length;
        public int OpenConnections => Connections.Count(p => p.Node == null);
        public int ClosedConnections => Connections.Count(p => p.Node != null);
        public bool IsClosed => !Connections.Any(p => p.Node == null);
        public int GetContentHash();
    }
    public abstract class Node<TKey, TValue> : IConnection<TKey, TValue> where TKey : notnull where TValue : IConnection<TKey, TValue>
    {
        public Node(IEnumerable<TKey> keys)
        {
            if (keys.ContainsAnyDuplicates()) throw new ArgumentException("Duplicate key");
            Connections = [.. keys.Select<TKey, NodeConnection<TKey, TValue>>(k => new(k))];
        }
        public NodeConnection<TKey, TValue>[] Connections { get; init; }
        public abstract int GetContentHash();
    }

    //Implement equals?
    public sealed class NodeInterconnect<TKey, TValue>(TKey connection) where TKey : notnull where TValue : IConnection<TKey, TValue>
    {
        public TKey Connection { get; init; } = connection;
        public TValue? NodeA { get; set; }
        public TValue? NodeB { get; set; }
        public override int GetHashCode() => Connection.GetHashCode();
    }
    public sealed class NodeConnection<TKey, TValue>(TKey key) where TKey : notnull where TValue : IConnection<TKey, TValue>
    {
        public TKey Key { get; init; } = key;
        public TValue? Node { get; set; }
    };

    /// <summary>
    /// A tree structure that can have leafs connect to other leafs(nodes)
    /// Must allow for pruning
    /// </summary>
    public class InosculationTree<TKey, TValue> where TKey : notnull where TValue : IConnection<TKey, TValue>
    {
        private TValue RootNode { get; init; }
        internal HashSet<NodeInterconnect<TKey, TValue>> AllConnections { get; init; }
        public bool IsOpen => AllConnections.Any(p => p.NodeA == null || p.NodeB == null);
        public Action<TKey, TValue>? OnConnect { get; init; }
        public Func<TKey, TValue, TValue, bool> CanConnect { get; init; }
        public Action<TKey, TValue> OnDisconnect { get; init; }
        public InosculationTree(TValue root, Func<TKey, TValue, TValue, bool>? canConnect = null, Action<TKey, TValue>? onConnect = null, Action<TKey, TValue>? onDisconnect = null)
        {
            RootNode = root;
            CanConnect = canConnect ?? new Func<TKey, TValue, TValue, bool>((_, _, _) => true);
            OnConnect = onConnect ?? new Action<TKey, TValue>((k, v) => { });
            OnDisconnect = onDisconnect ?? new Action<TKey, TValue>((k, v) => { });
            AllConnections = [];
            foreach (var n in RootNode.Connections)
            {
                if (!AllConnections.TryAdd(new NodeInterconnect<TKey, TValue>(n.Key) { NodeA = root, NodeB = default }))
                {
                    throw new ArgumentException("Duplicate key", n.Key.ToString());
                }
            }
        }
        public bool TryConnect(TKey connectionPoint, TValue parent, TValue child)
        {
            if (child == null) return false;
            if (!CanConnect(connectionPoint, parent, child)) return false;
            
            if (!parent.Connections.TryGetValue(connectionPoint, out var currentChild)) return false;
            if (!child.Connections.TryGetValue(connectionPoint, out var currentChildConnection)) return false;
            if (currentChild == null || currentChild.Node != null) return false;
            if (currentChildConnection == null || currentChildConnection.Node != null) return false;
            if (!AllConnections.TryGetValue(connectionPoint, out var connection)) return false;
            if (connection == null) return false;

            if (connection.NodeA == null && connection.NodeB != null)
            {
                connection.NodeA = connection.NodeB.Equals(parent) ? child : parent;
            }
            else if (connection.NodeB == null && connection.NodeA != null)
            {
                connection.NodeB = connection.NodeA.Equals(parent) ? child : parent;
            }
            else
            {
                return false;
            }

            currentChild.Node = child;
            currentChildConnection.Node = parent;

            foreach (var childConn in child.Connections)
            {
                if (childConn.Key.Equals(connectionPoint)) continue;
                AllConnections.TryAdd(new NodeInterconnect<TKey, TValue>(childConn.Key) { NodeA = child, NodeB = default });
            }

            OnConnect?.Invoke(connectionPoint, child);
            return true;
        }

        public bool TryPopulate(Func<TValue, TValue[]> availableNodes, int currentAttampts = 0, int maxAttempts = 500)
        {
            // Only clean up orphans that aren't connected to root, but keep root connections intact
            while (IsOpen && currentAttampts < maxAttempts)
            {
                currentAttampts++;

                var connectedNodes = GetNodesConnectedToRoot();

                var openConnection = AllConnections.FirstOrDefault(p =>
                    p.NodeA != null && connectedNodes.Contains(p.NodeA) && p.NodeB == null ||
                     p.NodeB != null && connectedNodes.Contains(p.NodeB) && p.NodeA == null);

                if (openConnection == null) return false;

                TValue? parent = openConnection.NodeA ?? openConnection.NodeB;
                if (parent == null)
                {
                    // Only remove if neither side is root
                    bool isRootConnection = openConnection.NodeA != null && openConnection.NodeA.Equals(RootNode) ||
                                           openConnection.NodeB != null && openConnection.NodeB.Equals(RootNode);

                    if (!isRootConnection)
                    {
                        AllConnections.Remove(openConnection);
                    }
                    continue;
                }

                if (!connectedNodes.Contains(parent))
                {
                    // Only remove if parent isn't root
                    if (!parent.Equals(RootNode))
                    {
                        AllConnections.Remove(openConnection);
                    }
                    continue;
                }
                var candidates = availableNodes(parent);

                if (candidates.Length == 0)
                {
                    if (parent.Equals(RootNode)) return false;

                    TValue? grandparent = default;
                    NodeInterconnect<TKey, TValue>? parentConnection = default;

                    foreach (var conn in AllConnections)
                    {
                        if (conn.NodeA != null && conn.NodeA.Equals(parent) && conn.NodeB != null)
                        {
                            parentConnection = conn;
                            grandparent = conn.NodeB;
                            break;
                        }
                        if (conn.NodeB != null && conn.NodeB.Equals(parent) && conn.NodeA != null)
                        {
                            parentConnection = conn;
                            grandparent = conn.NodeA;
                            break;
                        }
                    }

                    if (parentConnection != null && grandparent != null && !grandparent.Equals(RootNode))
                    {

                        if (!TryDisconnect(parentConnection.Connection, grandparent, parent)) return false;
                    }
                    continue;
                }

                bool connected = false;
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (TryConnect(openConnection.Connection, parent, candidates[i]))
                    {
                        connected = true;
                        break;
                    }
                }

                if (!connected)
                {
                    if (parent.Equals(RootNode))
                    {
                        // All candidates failed for this root connection
                        // Don't disconnect anything from root, just continue to try other connections
                        continue;
                    }

                    // Backtrack for non-root parent
                    TValue? grandparent = default;
                    NodeInterconnect<TKey, TValue>? parentConnection = default;

                    foreach (var conn in AllConnections)
                    {
                        if (conn.NodeA != null && conn.NodeA.Equals(parent) && conn.NodeB != null)
                        {
                            parentConnection = conn;
                            grandparent = conn.NodeB;
                            break;
                        }
                        if (conn.NodeB != null && conn.NodeB.Equals(parent) && conn.NodeA != null)
                        {
                            parentConnection = conn;
                            grandparent = conn.NodeA;
                            break;
                        }
                    }

                    if (parentConnection != null && grandparent != null)
                    {
                        if (!TryDisconnect(parentConnection.Connection, grandparent, parent)) return false;
                    }
                }
            }

            // Final verification - don't call RemoveAllOrphanedNodes
            var finalConnectedNodes = GetNodesConnectedToRoot();

            var stillOpen = AllConnections.Any(p =>
                p.NodeA != null && finalConnectedNodes.Contains(p.NodeA) && p.NodeB == null ||
                 p.NodeB != null && finalConnectedNodes.Contains(p.NodeB) && p.NodeA == null);

            return !stillOpen;
        }
        public bool TryDisconnect(TKey connectionPoint, TValue parent, TValue child)
        {

            if (child == null || parent == null) return false;

            if (!parent.Connections.TryGetValue(connectionPoint, out var parentConnection)) return false;
            if (!child.Connections.TryGetValue(connectionPoint, out var childConnection)) return false;
            if (parentConnection == null || childConnection == null) return false;
            if (parentConnection.Node == null || !parentConnection.Node.Equals(child)) return false;
            if (childConnection.Node == null || !childConnection.Node.Equals(parent)) return false;

            if (!AllConnections.TryGetValue(connectionPoint, out var interconnect)) return false;
            if (interconnect == null) return false;

            // IMPORTANT: Find all nodes that will become orphaned BEFORE we disconnect
            // This is because once we disconnect, we can't traverse to find them anymore
            var nodesToRemove = GetSubtreeNodes(child, parent);

            // Now perform the disconnect
            parentConnection.Node = default;
            childConnection.Node = default;

            if (interconnect.NodeA != null && interconnect.NodeA.Equals(child))
            {
                OnDisconnect(interconnect.Connection, interconnect.NodeA);
                interconnect.NodeA = default;
            }
            else if (interconnect.NodeB != null && interconnect.NodeB.Equals(child))
            {
                OnDisconnect(interconnect.Connection, interconnect.NodeB);
                interconnect.NodeB = default;
            }

            // Clean up all orphaned nodes and their interconnects
            CleanupOrphanedNodes(nodesToRemove);

            return true;
        }

        // Get all nodes in the subtree rooted at 'node', excluding 'excludeParent'
        private static HashSet<TValue> GetSubtreeNodes(TValue node, TValue excludeParent)
        {
            HashSet<TValue> subtree = [];
            Queue<TValue> queue = new();
            queue.Enqueue(node);
            subtree.Add(node);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var conn in current.Connections)
                {
                    if (conn.Node == null) continue;
                    if (conn.Node.Equals(excludeParent)) continue; // Don't traverse back to parent

                    if (!subtree.Contains(conn.Node))
                    {
                        subtree.Add(conn.Node);
                        queue.Enqueue(conn.Node);
                    }
                }
            }

            return subtree;
        }

        private void CleanupOrphanedNodes(HashSet<TValue> orphanedNodes)
        {
            var interconnectsToRemove = new HashSet<NodeInterconnect<TKey, TValue>>();

            // First pass: disconnect all orphaned nodes from their connections
            foreach (var orphan in orphanedNodes)
            {
                foreach (var conn in orphan.Connections)
                {
                    if (conn.Node != null)
                    {
                        // Clear the other side's reference
                        if (conn.Node.Connections.TryGetValue(conn.Key, out var otherConn) && otherConn != null)
                        {
                            if (otherConn.Node != null && otherConn.Node.Equals(orphan))
                            {
                                OnDisconnect(conn.Key, otherConn.Node);
                                otherConn.Node = default;
                            }
                        }

                        OnDisconnect(conn.Key, conn.Node);
                        conn.Node = default;
                    }
                }
            }

            // Second pass: clean up interconnects
            foreach (var interconnect in AllConnections.ToList())
            {
                bool nodeAIsOrphan = interconnect.NodeA != null && orphanedNodes.Contains(interconnect.NodeA);
                bool nodeBIsOrphan = interconnect.NodeB != null && orphanedNodes.Contains(interconnect.NodeB);

                if (nodeAIsOrphan || nodeBIsOrphan)
                {
                    if (nodeAIsOrphan)
                    {
                        interconnect.NodeA = default;
                    }
                    if (nodeBIsOrphan)
                    {
                        interconnect.NodeB = default;
                    }

                    // If both sides are now null, remove the interconnect
                    if (interconnect.NodeA == null && interconnect.NodeB == null)
                    {
                        interconnectsToRemove.Add(interconnect);
                    }
                }
            }

            foreach (var interconnect in interconnectsToRemove)
            {
                AllConnections.Remove(interconnect);
            }
        }

        private HashSet<TValue> GetNodesConnectedToRoot()
        {
            HashSet<TValue> reachable = [RootNode];
            Queue<TValue> queue = new();
            queue.Enqueue(RootNode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var conn in current.Connections)
                {
                    if (conn.Node == null) continue;

                    if (!reachable.Contains(conn.Node))
                    {
                        reachable.Add(conn.Node);
                        queue.Enqueue(conn.Node);
                    }
                }
            }

            return reachable;
        }
        
        public bool TryPopulateRecursive(Func<TValue, TValue[]> availableNodes, int maxDepth = 1000)
        {
           var attemptedConnections = new HashSet<string>();
           return PopulateRecursiveInternal(RootNode, availableNodes, attemptedConnections, 0, maxDepth);
        }

        
        private bool PopulateRecursiveInternal(
            TValue currentNode,
            Func<TValue, TValue[]> availableNodes,
            HashSet<string> attemptedConnections,
            int depth,
            int maxDepth)
        {
            // Prevent infinite recursion
            if (depth > maxDepth) return false;

            // Base case: if tree is closed, we're done
            if (!IsOpen) return true;

            // Find the first open connection for this node
            var openConnection = currentNode.Connections.FirstOrDefault(c => c.Node == null);

            // If this node has no open connections, it's fully connected - find next open node
            if (openConnection == null)
            {
                var connectedNodes = GetNodesConnectedToRoot();
                var globalOpenConnection = AllConnections.FirstOrDefault(p =>
                    p.NodeA != null && connectedNodes.Contains(p.NodeA) && p.NodeB == null ||
                     p.NodeB != null && connectedNodes.Contains(p.NodeB) && p.NodeA == null);

                if (globalOpenConnection == null) return !IsOpen;

                var nextParent = globalOpenConnection.NodeA ?? globalOpenConnection.NodeB;
                if (nextParent == null) return false;

                // Continue from the next open node WITHOUT incrementing depth
                // This is forward progress, not backtracking
                return PopulateRecursiveInternal(nextParent, availableNodes, attemptedConnections, depth, maxDepth);
            }

            // Create unique key for this connection attempt
            var connectionKey = $"{openConnection.Key.GetHashCode()}-{currentNode.GetHashCode()}";

            // Get candidates for this connection - get them ONCE per connection
            var candidates = availableNodes(currentNode);
            if(candidates == null || candidates.Length==0) return false;

            // Important: Don't shuffle here! The candidates should be in a consistent order
            // If availableNodes is returning shuffled results, it can cause the algorithm to miss valid solutions

            // Try each candidate
            foreach (var candidate in candidates)
            {
                // Create unique attempt key
                var attemptKey = $"{connectionKey}-{candidate.GetContentHash()}";

                // Skip if we've already tried this exact combination and it failed
                if (attemptedConnections.Contains(attemptKey)) continue;

                // Try to connect
                if (TryConnect(openConnection.Key, currentNode, candidate))
                {
                    // After successful connection, always continue forward
                    // The recursive call will find the next open node (could be candidate or elsewhere)
                    bool success = PopulateRecursiveInternal(candidate, availableNodes, attemptedConnections, depth + 1, maxDepth);

                    if (success)
                    {
                        return true; // Success - keep this connection!
                    }
                    // Always disconnect and try next candidate — the subtree failed
                    attemptedConnections.Add(attemptKey);
                    if (!TryDisconnect(openConnection.Key, currentNode, candidate)) return false;
                    /*
                    // Only disconnect and try next candidate if we're still trying to solve THIS node's connection
                    // Don't disconnect if the failure was deeper in the tree
                    var stillHasOpenConnections = currentNode.Connections.Any(c => c.Node == null);

                    if (stillHasOpenConnections)
                    {
                        // Current node still has open connections, so the failure was with this specific candidate
                        // Mark as attempted and disconnect to try next candidate
                        attemptedConnections.Add(attemptKey);

                        if (!TryDisconnect(openConnection.Key, currentNode, candidate))
                        {
                            return false; // Fatal error
                        }
                    }
                    else
                    {
                        // Current node is fully closed, failure was elsewhere in the tree
                        // DON'T disconnect - this connection is good, we just need to complete other parts
                        // Return false to signal we need to try a different path elsewhere
                        return false;
                    }*/
                }
                else
                {
                    // Connection failed, mark as attempted
                    attemptedConnections.Add(attemptKey);
                }
            }

            // All candidates exhausted for this connection
            if (currentNode.Equals(RootNode))
            {
                return false;
            }

            return false;
        }
    }
}
