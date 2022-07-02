using System.Collections.Generic;
using UnityEngine;

public class GraphManager : MonoBehaviour
{
    public GameObject cubeNode;

    private static GraphManager singltone;
    private List<List<Node>> parts;
    private Material lineMaterial;

    public static GraphManager Get()
    {
        if (singltone is null)
        {
            var gameObject = GameObject.Find("GRAPH_MANAGER");
            singltone = gameObject.GetComponentInChildren<GraphManager>();
            singltone.lineMaterial = new Material(Shader.Find("Sprites/Default"));
        }
        return singltone;
    }

    public void Init(List<Node> sceneNodes)
    {
        var volume = new Volume();
        parts = Node.FindIsolatedGraphs(sceneNodes);
        for (var i = 0; i < parts.Count; i++)
        {
            var nodes = parts[i];
            Node.AlignNodes(nodes);
            Node.AlignNodesByForceDirected(nodes);
            CreateGameObjectsFromNodes(nodes, i, volume);
        }

        var orbit = Camera.main.GetComponent<DragMouseOrbit>();
        orbit.target = volume.GetCenter();
        orbit.distance = volume.GetRadius() * 2;
    }

    public List<List<Node>> GetParts()
    {
        return parts;
    }

    private void CreateGameObjectsFromNodes(List<Node> nodes, int offset, Volume volume)
    {
        var definitions = new Dictionary<Node, GameObject>();
        var links = new HashSet<(Node from, Node to)>();
        foreach (var node in nodes)
        {
            var position = (node.position + new Vector3(0, 0, offset)) * 2;
            position.y = -position.y;
            node.gameObject = Instantiate(cubeNode, position, Quaternion.identity);
            volume.Add(node.gameObject);
            var textMesh = node.gameObject.GetComponentInChildren<TextMesh>();
            textMesh.text = node.text;
            textMesh.gameObject.AddComponent<LookAtCamera>();
            definitions[node] = node.gameObject;
            foreach (var input_node in node.inputs)
                links.Add((from: input_node, to: node));
            foreach (var output_node in node.outputs)
                links.Add((from: node, to: output_node));
        }
        foreach (var (from, to) in links)
            LineTo(definitions[from], definitions[to]);
    }

    public void LinkNode(Node node, Node target, List<Node> graph)
    {
        target.outputs.Add(node);
        node.inputs.Add(target);
        graph.Add(node);

        Node.AlignNodesByForceDirected(graph);

        node.gameObject = Instantiate(cubeNode);
        var textMesh = node.gameObject.GetComponentInChildren<TextMesh>();
        textMesh.text = node.text;

        // clear links
        foreach (var n in graph)
        {
            var lineRenderer = n.gameObject.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 0;
            }

        }

        // set positions and links
        var definitions = new Dictionary<Node, GameObject>();
        var links = new HashSet<(Node from, Node to)>();
        foreach (var n in graph)
        {
            var position = n.position;
            position.y = -position.y;
            n.gameObject.transform.position = position;

            definitions[n] = n.gameObject;
            foreach (var input_node in n.inputs)
                links.Add((from: input_node, to: n));
            foreach (var output_node in n.outputs)
                links.Add((from: n, to: output_node));
        }
        foreach (var (from, to) in links)
            LineTo(definitions[from], definitions[to]);
    }

    private void LineTo(GameObject start, GameObject stop) {
        var lineRenderer = start.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = start.AddComponent<LineRenderer>();
            lineRenderer.material = lineMaterial;
            lineRenderer.widthMultiplier = 0.15f;
            lineRenderer.positionCount = 0;
        }
        lineRenderer.positionCount += 2;
        lineRenderer.SetPosition(lineRenderer.positionCount - 2, start.transform.position);
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, stop.transform.position);
    }
}

class Volume
{
    public Vector3 Min = new(float.MaxValue, float.MaxValue, float.MaxValue);
    public Vector3 Max = new(float.MinValue, float.MinValue, float.MinValue);

    public void Add(GameObject gameObject)
    {
        var position = gameObject.transform.position;

        if (Min.x > position.x)
            Min.x = position.x;
        if (Min.y > position.y)
            Min.y = position.y;
        if (Min.z > position.z)
            Min.z = position.z;

        if (Max.x < position.x)
            Max.x = position.x;
        if (Max.y < position.y)
            Max.y = position.y;
        if (Max.z < position.z)
            Max.z = position.z;
    }

    public Vector3 GetCenter()
    {
        return (Max + Min) / 2;
    }

    public float GetRadius()
    {
        return ((Max - Min) / 2).magnitude;
    }
}
