using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    //  Модель для отрисовки узла сетки
    public GameObject nodeModel;

    //  Ландшафт (Terrain) на котором строится путь
    [SerializeField] private Terrain landscape = null;

    //  Шаг сетки (по x и z) для построения точек
    [SerializeField] private int gridDelta = 100;

    //  Номер кадра, на котором будет выполнено обновление путей
    private int updateAtFrame = 0;  

    //  Массив узлов - создаётся один раз, при первом вызове скрипта
    private PathNode[,] grid = null;

    private void CheckWalkableNodes()
    {
        foreach (PathNode node in grid)
        {
            //  Пока что считаем все вершины проходимыми, без учёта препятствий
            //node.walkable = true;

            node.walkable = !Physics.CheckSphere(node.body.transform.position, 1);
            if (node.walkable)
                node.Fade();
            else
            {
                node.Bad();
            }
        }
    }


    // Метод вызывается однократно перед отрисовкой первого кадра
    void Start()
    {
        //  Создаём сетку узлов для навигации - адаптивную, под размер ландшафта
        Vector3 terrainSize = landscape.terrainData.bounds.size;
        int sizeX = (int)(terrainSize.x / gridDelta);
        int sizeZ = (int)(terrainSize.z / gridDelta);
        //  Создаём и заполняем сетку вершин, приподнимая на 25 единиц над ландшафтом
        grid = new PathNode[sizeX,sizeZ];
        for (int x = 0; x < sizeX; ++x)
            for (int z = 0; z < sizeZ; ++z)
            {
                Vector3 position = new Vector3(x * gridDelta, 0, z * gridDelta);
                position.y = landscape.SampleHeight(position) + 25;
                grid[x, z] = new PathNode(nodeModel, false, position, new Vector2Int(x, z));
                grid[x, z].ParentNode = null;
                grid[x, z].Fade();
            }
    }

    /// <summary>
    /// Получение списка соседних узлов для вершины сетки
    /// </summary>
    /// <param name="current">индексы текущей вершины </param>
    /// <returns></returns>
    private List<Vector2Int> GetNeighbours(Vector2Int current)
    {
        List<Vector2Int> nodes = new List<Vector2Int>();
        for (int x = current.x - 1; x <= current.x + 1; ++x)
            for (int y = current.y - 1; y <= current.y + 1; ++y)
                if (x >= 0 && y >= 0 && x < grid.GetLength(0) && y < grid.GetLength(1) && (x != current.x || y != current.y))
                    nodes.Add(new Vector2Int(x, y));
                return nodes;
    }

    /// <summary>
    /// Вычисление "кратчайшего" между двумя вершинами сетки
    /// </summary>
    /// <param name="startNode">Координаты начального узла пути (индексы элемента в массиве grid)</param>
    /// <param name="finishNode">Координаты конечного узла пути (индексы элемента в массиве grid)</param>
    void calculatePath(Vector2Int startNode, Vector2Int finishNode)
    {
        foreach (var node in grid)
        {
            node.Fade();
            node.ParentNode = null;
        }
        
        CheckWalkableNodes();

        PathNode start = grid[startNode.x, startNode.y];

        start.ParentNode = null;
        start.Distance = 0;
        
        Queue<Vector2Int> nodes = new Queue<Vector2Int>();

        nodes.Enqueue(startNode);

        while(nodes.Count != 0)
        {
            Vector2Int current = nodes.Dequeue();

            if (current == finishNode) break;

            var neighbours = GetNeighbours(current);
            foreach (var node in neighbours)
                if(grid[node.x, node.y].walkable && grid[node.x, node.y].Distance > grid[current.x, current.y].Distance + PathNode.Dist(grid[node.x, node.y], grid[current.x, current.y]))
                {
                    grid[node.x, node.y].ParentNode = grid[current.x, current.y];
                    nodes.Enqueue(node);
                }
        }

        var pathElem = grid[finishNode.x, finishNode.y];
        while(pathElem != null)
        {
            pathElem.Illuminate();
            pathElem = pathElem.ParentNode;
        }
    }

    void calculatePathAStar(Vector2Int startNode, Vector2Int finishNode)
    {
        foreach (var node in grid)
        {
            node.Fade();
            node.ParentNode = null;
        }

        CheckWalkableNodes();

        PathNode start = grid[startNode.x, startNode.y];

        start.ParentNode = null;
        start.Distance = 0;

        var nodes = new PriorityQueue<PathNode>();

        nodes.Enqueue(start);

        while (nodes.Count != 0)
        {
            PathNode current = nodes.Dequeue();
            Vector2Int currentCoord = current.getGridCoord();

            if (currentCoord == finishNode) break;

            var neighbours = GetNeighbours(currentCoord);


            foreach (var node in neighbours)
            {
                if (grid[node.x, node.y].walkable && grid[node.x, node.y].Distance > grid[currentCoord.x, currentCoord.y].Distance + PathNode.Dist(grid[node.x, node.y], grid[currentCoord.x, currentCoord.y]) + PathNode.Dist(grid[node.x, node.y], grid[finishNode.x, finishNode.y]))
                {
                    grid[node.x, node.y].ParentNode = grid[currentCoord.x, currentCoord.y];
                    grid[node.x, node.y].AddAStarDistance(grid[finishNode.x, finishNode.y]);
                    nodes.Enqueue(grid[node.x, node.y]);
                }
            }
        }

        var pathElem = grid[finishNode.x, finishNode.y];
        while(pathElem != null)
        {
            pathElem.Illuminate();
            pathElem = pathElem.ParentNode;
        }
    }

    // Метод вызывается каждый кадр
    void Update()
    {
        //  Чтобы не вызывать этот метод каждый кадр, устанавливаем интервал вызова в 1000 кадров
        if (Time.frameCount < updateAtFrame) return;
        updateAtFrame = Time.frameCount + 300;

        calculatePathAStar(new Vector2Int(0, 0), new Vector2Int(grid.GetLength(0)-1, grid.GetLength(1)-1));
    }
}
