using System.Net.Mime;
using System.Runtime.InteropServices;
using Kse.Algorithms.Samples;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

class Program
{
    static void Main(string[] args)
    {


        var generator = new MapGenerator(new MapGeneratorOptions()
        {
            Height = 35,
            Width = 90,
            Seed = 25,
            AddTraffic = true,
            TrafficSeed = 12345
        });

        string[,] map = generator.Generate();
        new MapPrinter().Print(map);


        Console.WriteLine("Type the starting point (col, row) >>");
        int startCol = Convert.ToInt32(Console.ReadLine());
        int startRow = Convert.ToInt32(Console.ReadLine());

        Console.WriteLine("Type the finishing point (col, row) >>");
        int finCol = Convert.ToInt32(Console.ReadLine());
        int finRow = Convert.ToInt32(Console.ReadLine());

        var start = new Point(startCol, startRow);
        var finish = new Point(finCol, finRow);

        var finder = new MinHeap.DijikstraFinder(map, start, finish);
        List<Point> path = finder.FindPath();

        if (path.Count > 0)
        {
     
            Console.WriteLine("Your path is FOUND!");
            foreach (var point in path)
            {
                Console.WriteLine($"{point.Column}, {point.Row}");
                if (!point.Equals(start) && !point.Equals(finish))
                {
                    map[point.Column, point.Row] = "*";
                }
                else if (point.Equals(start))
                {
                    map[point.Column, point.Row] = "A";
                }
                else if (point.Equals(finish))
                {
                    map[point.Column, point.Row] = "B";
                }
            }
        }
        else
            {
                Console.WriteLine("Path is NOT FOUND!");
            }
        new MapPrinter().Print(map);
        }
    }


// node
public struct Node
{
    public double Cost;
    public Point Position;

    public Node(Point position, double cost)
    {
        Cost = cost;
        Position = position;
    }
}

// heap
public class MinHeap
{
    private Node[] elements;
    private int size;
    private Dictionary<Point, int> positions;
    
    public MinHeap(int maxSize)
    {
        elements = new Node[maxSize]; // initialize heap with the elements number = size
        positions = new Dictionary<Point, int>();
    }
    
    // complementary methods 
    private int GetLeftChildIndex(int elementIndex) => 2 * elementIndex + 1;
    private int GetRightChildIndex(int elementIndex) => 2 * elementIndex + 2;
    private int GetParentIndex(int elementIndex) => (elementIndex - 1) / 2;

    private bool HasLeftChild(int elementIndex) => GetLeftChildIndex(elementIndex) < size;
    private bool HasRightChild(int elementIndex) => GetRightChildIndex(elementIndex) < size;
    private bool IsRoot(int elementIndex) => elementIndex == 0;
    
    public bool IsEmpty() => size == 0;

    public Node Peek() // return min element (root)
    {
        if (size == 0)
        {
            throw new Exception("Nothing to peek!");
        }

        return elements[0]; // return root
    }

    public Node Pop() // get min element (root)
    {
        if (size == 0)
        {
            throw new Exception("Nothing to pop!");
        }

        var root = elements[0]; // root
        elements[0] = elements[--size]; 
        positions[elements[0].Position] = 0; // set new root
        positions.Remove(root.Position);
        HeapifyDown();
        
        return root;
    }

    public void Add(Node element) //add new node
    {
        if (size == elements.Length)
            throw new Exception("Exceeded number of cells!");
        elements[size] = element;
        positions[element.Position] = size; 
        HeapifyUp(size++); // reassembly the tree to add new elements
    }
    
    // heapify 
    private void HeapifyDown() // when pop 
    {
        int index = 0;
        while (HasLeftChild(index))
        {
            var smallerChildIndex = GetLeftChildIndex(index);
            if (HasRightChild(index) && elements[GetRightChildIndex(index)].Cost < elements[smallerChildIndex].Cost)
            {
                smallerChildIndex = GetRightChildIndex(index);
            }

            if (elements[index].Cost <= elements[smallerChildIndex].Cost)
            {
                break; 
            }
            Swap(index, smallerChildIndex);
            index = smallerChildIndex;
        }
    }

    private void HeapifyUp(int index) // when add
    {
        while (!IsRoot(index) && elements[index].Cost < elements[GetParentIndex(index)].Cost)
        {
            Swap(index, GetParentIndex(index));
            index = GetParentIndex(index);
        }
    }

    public void DecreaseKey(Point position, int newCost)
    {
        if (!positions.TryGetValue(position, out int index))
        {
            throw new Exception("Element is not found!");
        }

        elements[index].Cost = newCost;
        HeapifyUp(index);
    }

    private void Swap(int firstIndex, int secondIndex)
    {
        (elements[firstIndex], elements[secondIndex]) = (elements[secondIndex], elements[firstIndex]);

        positions[elements[firstIndex].Position] = firstIndex;
        positions[elements[secondIndex].Position] = secondIndex;
    }

    public class DijikstraFinder
    {
        private string[,] maze;
        private Point start, finish;
        private Dictionary<Point, double> distances;
        private Dictionary<Point, Point?> predecessors;
        private MinHeap openSet;

        public DijikstraFinder(string[,] maze, Point start, Point finish)
        {
            this.maze = maze;
            this.start = start;
            this.finish = finish;
            distances = new Dictionary<Point, double>();
            predecessors = new Dictionary<Point, Point?>();
            openSet = new MinHeap(maze.GetLength(0) * maze.GetLength(1));


        }
        

        public List<Point> FindPath()
        
        {
            for (int y = 0; y < maze.GetLength(1); y++)
            {
                for (int x = 0; x < maze.GetLength(0); x++)
                {
                    var point = new Point(x, y);
                    distances[point] = double.MaxValue;
                    predecessors[point] = null;
                }
            }

            distances[start] = 0;


            var startNode = new Node(start, 0);

            openSet.Add(startNode);
            
            while (!openSet.IsEmpty())
            {
                var currentNode = openSet.Pop();
                
                if (currentNode.Position.Equals(finish))
                {
                    return ReconstructPath(predecessors, finish);
                }

                
                foreach (var neighbor in GetNeighbors(currentNode.Position))
                {
                    int traffic = 1;
                    if (maze[neighbor.Column, neighbor.Row] != MapGenerator.Wall)
                    {
                        traffic = int.TryParse(maze[neighbor.Column, neighbor.Row], out int parsedTraffic)
                            ? parsedTraffic
                            : 1; 

                    }

                    double newCost = distances[currentNode.Position] + ((distances[currentNode.Position] + 1) / CalculateSpeed(traffic));

                    if (distances.ContainsKey(neighbor))
                    {
                        if (newCost < distances[neighbor])
                        {
                            distances[neighbor] = newCost;
                            predecessors[neighbor] = currentNode.Position;

                            openSet.Add(new Node(neighbor, newCost));

                        }

                    }
                    else
                    {
                        
                    }
                }
            }

            if (distances[finish] != null)
            {
                Console.WriteLine($"Total time is {distances[finish]}");
            }
            
            return new List<Point>();
        }

        double CalculateSpeed(int traffic)
        {
            return 60 - (traffic - 1) * 6;
        }
        private List<Point> GetNeighbors(Point point)
        {
            var neighbors = new List<Point>();
            var directions = new Point[] { new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0) };

            foreach (var dir in directions)
            {
                var nextPoint = new Point(point.Column + dir.Column, point.Row + dir.Row);
                if (nextPoint.Column >= 0 && nextPoint.Column < maze.GetLength(0) && nextPoint.Row >= 0 &&
                    nextPoint.Row < maze.GetLength(1) && maze[nextPoint.Column, nextPoint.Row] != MapGenerator.Wall)
                {
                    neighbors.Add(nextPoint);
                }
            }

            return neighbors;
        }

        private List<Point> ReconstructPath(Dictionary<Point, Point?> predecessors, Point current)
        {
            var path = new List<Point>();
            Point? currentPredecessor = current;
            while (currentPredecessor != null)
            {
                path.Insert(0, currentPredecessor.Value);
                
                currentPredecessor = predecessors[currentPredecessor.Value];
            }

            Console.WriteLine($"Total time is {distances[finish]} hours");

            return path;
        }
        
    }
}
    