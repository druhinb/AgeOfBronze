public class Edge {
    // Start and end nodes
    public Node startNode;
    public Node endNode;

    // Create a link
    public Edge(Node from, Node to) {
        startNode = from;
        endNode = to;
    }
}
