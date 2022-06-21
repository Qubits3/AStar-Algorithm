using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathMarker
{
    public MapLocation location;
    public float G;
    public float H;
    public float F;
    public GameObject marker;
    public PathMarker parent;

    public PathMarker(MapLocation l, float g, float h, float f, GameObject marker, PathMarker p)
    {
        location = l;
        G = g;
        H = h;
        F = f;
        this.marker = marker;
        parent = p;
    }

    public override bool Equals(object obj)
    {
        if ((obj == null) || !this.GetType().Equals(obj.GetType()))
        {
            return false;
        }
        else
        {
            return location.Equals(((PathMarker)obj).location);
        }
    }

    public override int GetHashCode()
    {
        return 0;
    }
}

public class FindPathAStar : MonoBehaviour
{
    public Maze maze;
    public Material closedMaterial;
    public Material openMaterial;

    private List<PathMarker> open = new List<PathMarker>();
    private List<PathMarker> closed = new List<PathMarker>();

    public GameObject startPrefab;
    public GameObject endPrefab;
    public GameObject pathPrefab;

    private PathMarker goalNode;
    private PathMarker startNode;

    private PathMarker lastPos;
    private bool done = false;

    private void RemoveAllMarkers()
    {
        GameObject[] markers = GameObject.FindGameObjectsWithTag("marker");
        foreach (GameObject marker in markers)
        {
            Destroy(marker);
        }
    }

    private void BeginSearch()
    {
        done = false;
        RemoveAllMarkers();

        List<MapLocation> locations = new List<MapLocation>();
        for (int z = 1; z < maze.depth - 1; z++)
            for (int x = 1; x < maze.width - 1; x++)    // search for all maze
            {
                if (maze.map[x, z] != 1)    // if it's not wall
                    locations.Add(new MapLocation(x, z));   // add all locations to the {locations}
            }

        locations.Shuffle();

        // add start marker
        Vector3 startLocation = new Vector3(locations[0].x * maze.scale, 0, locations[0].z * maze.scale);
        startNode = new PathMarker(new MapLocation(locations[0].x, locations[0].z), 0.0f, 0.0f, 0.0f,
            Instantiate(startPrefab, startLocation, Quaternion.identity), null);

        // add goal marker
        Vector3 goalLocation = new Vector3(locations[1].x * maze.scale, 0, locations[1].z * maze.scale);
        goalNode = new PathMarker(new MapLocation(locations[1].x, locations[1].z), 0.0f, 0.0f, 0.0f,
            Instantiate(endPrefab, goalLocation, Quaternion.identity), null);

        open.Clear();
        closed.Clear();
        open.Add(startNode);
        lastPos = startNode;
    }

    private void Search(PathMarker thisNode)
    {
        if (thisNode.Equals(goalNode)) { done = true; return; } // goal has been found

        foreach (MapLocation dir in maze.directions)    // loop through neighbours
        {
            MapLocation neighbour = dir + thisNode.location;

            if (maze.map[neighbour.x, neighbour.z] == 1) continue;  // if it's a wall
            if (neighbour.x < 1 || neighbour.x >= maze.width || neighbour.z < 1 || neighbour.z >= maze.depth) continue;  // if it's outside the maze
            if (IsClosed(neighbour)) continue;  // if it's in closed list

            float G = Vector2.Distance(thisNode.location.ToVector(), neighbour.ToVector()) + thisNode.G;
            float H = Vector2.Distance(neighbour.ToVector(), goalNode.location.ToVector());
            float F = G + H;

            GameObject pathBlock = Instantiate(pathPrefab,
                new Vector3(neighbour.x * maze.scale, 0, neighbour.z * maze.scale),
                Quaternion.identity);

            TextMesh[] values = pathBlock.GetComponentsInChildren<TextMesh>();
            values[0].text = "G: " + G.ToString("0.00");
            values[1].text = "H: " + H.ToString("0.00");
            values[2].text = "F: " + F.ToString("0.00");

            if (!UpdateMarker(neighbour, G, H, F, thisNode))  // if it's not in open list (if it's not calculated before)
                open.Add(new PathMarker(neighbour, G, H, F, pathBlock, thisNode));
        }

        open = open.OrderBy(p => p.F).ToList();
        PathMarker pm = open.ElementAt(0);
        closed.Add(pm);

        open.RemoveAt(0);
        pm.marker.GetComponent<Renderer>().material = closedMaterial;

        // The node that has the lowest F value
        lastPos = pm;
    }

    private bool IsClosed(MapLocation marker)
    {
        foreach (PathMarker path in closed)
        {
            if (path.location.Equals(marker)) return true;
        }

        return false;
    }

    private bool UpdateMarker(MapLocation pos, float g, float h, float f, PathMarker prt)
    {
        foreach (PathMarker pathMarker in open)
        {
            if (pathMarker.location.Equals(pos))
            {
                pathMarker.G = g;
                pathMarker.H = h;
                pathMarker.F = f;
                pathMarker.parent = prt;
                return true;
            }
        }

        return false;
    }

    private void GetPath()
    {
        RemoveAllMarkers();
        PathMarker begin = lastPos;

        while (!startNode.Equals(begin) && begin != null)
        {
            Instantiate(pathPrefab, new Vector3(begin.location.x * maze.scale, 0, begin.location.z * maze.scale), Quaternion.identity);

            begin = begin.parent;
        }

        Instantiate(pathPrefab, new Vector3(startNode.location.x * maze.scale, 0, startNode.location.z * maze.scale), Quaternion.identity);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            BeginSearch();
        }

        if (Input.GetKeyDown(KeyCode.C) && !done)
        {
            Search(lastPos);
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            GetPath();
        }
    }
}
