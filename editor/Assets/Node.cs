﻿using UnityEngine;
using System.Collections.Generic;

public class Node
{
    public readonly GameObject gameObject;
    public List<Node> inputs = new List<Node>();
    public List<Node> outputs = new List<Node>();
    public Vector3 position;
    public string text;
    public NodeType type;

    public static List<List<Node>> FindIsolatedGraphs(List<Node> nodes)
    {
        var result = new List<List<Node>>();
        var pass = new HashSet<Node>();
        foreach (var node in nodes)
        {
            if (pass.Contains(node))
                continue;
            var subgraph = new List<Node>();
            FindSubgraph(node, subgraph);
            result.Add(subgraph);
            pass.UnionWith(subgraph);
        }
        return result;
    }

    private static void FindSubgraph(Node node, List<Node> result)
    {
        if (result.Contains(node))
            return;
        result.Add(node);
        foreach (var input_node in node.inputs)
        {
            FindSubgraph(input_node, result);
        }
        foreach (var output_node in node.outputs)
        {
            FindSubgraph(output_node, result);
        }
    }

    public static Node GetStartNode(List<Node> nodes)
    {
        var e = nodes.GetEnumerator();
        e.MoveNext();
        var result = e.Current;
        while (e.MoveNext())
        {
            var node = e.Current;
            if (node.inputs.Count < result.inputs.Count && node.outputs.Count != 0)
            {
                result = node;
            }
        }
        return result;
    }

    public static void AlignNodes(List<Node> nodes)
    {
        // set levels
        var startNode = GetStartNode(nodes);
        var levels = new Dictionary<Node, int>
        {
            [startNode] = 0
        };
        var future = new HashSet<Node>
        {
            startNode
        };
        var pass = new HashSet<Node>();
        while (future.Count != 0)
        {
            var newFuture = new HashSet<Node>();
            foreach (var node in future)
            {
                var level = levels[node];
                foreach (var input_node in node.inputs)
                {
                    if (!pass.Contains(input_node))
                    {
                        levels[input_node] = level - 1;
                        newFuture.Add(input_node);
                    }
                }
                foreach (var output_node in node.outputs)
                {
                    if (!pass.Contains(output_node))
                    {
                        levels[output_node] = level + 1;
                        newFuture.Add(output_node);
                    }
                }
                pass.Add(node);
            }
            future = newFuture;
        }

        // group by level
        var group_by_level = new SortedDictionary<int, List<Node>>();
        foreach (var pair in levels)
        {
            if (!group_by_level.ContainsKey(pair.Value))
            {
                group_by_level[pair.Value] = new List<Node>();
            }
            group_by_level[pair.Value].Add(pair.Key);
        }

        // set positions
        foreach (var pair in group_by_level)
        {
            var offset = new Vector3(-pair.Value.Count / 2.0f, 0, 0);
            int i = 0;
            foreach (var node in pair.Value)
            {
                node.position = new Vector3(i, pair.Key) + offset;
                i++;
            }
        }
    }
}

public enum NodeType
{
    Input,
    Output,
    If,
    Constant,
    Operator,
    Subspace,
    SelfSubspace
}