using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    public static MazeGenerator Instance;

    [Header("미로 설정")]
    public int width = 10;
    public int height = 10;
    public GameObject cellPrefab;
    public float cellSize = 2f;

    [Header("시각화 설정")]
    public bool visualizeGeneration = false;                                    //생성 과정 보기
    public float viaulizationSpeed = 0.05f;                                     //속도
    public Color visitedColor = Color.cyan;                                     //방문한 칸 색상
    public Color currentColor = Color.yellow;                                   //현재 칸 색상
    public Color backtrackColor = Color.magenta;                                //뒤로 가기 색상

    private MazeCell[,] maze;
    private Stack<MazeCell> cellStack;                                          //DFS를 위한 스택 


    // Start is called before the first frame update
    void Start()
    {
        GenerateMaze();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void GenerateMaze()
    {
        maze = new MazeCell[width, height];
        cellStack = new Stack<MazeCell>();

        CreateCells();                                                          //모든 셀 생성

        if (visualizeGeneration)
        {
            StartCoroutine(GenerateWithDFSVisualized());
        }
        else
        {
            GenerateWithDFS();
        }
    }

    void GenerateWithDFS()                                                      //DFS 알고리즘으로 생성
    {
        MazeCell current = maze[0, 0];
        current.visited = true;
        cellStack.Push(current);                                                //첫번 현재칸을 스택에 넣는다. 

        while (cellStack.Count > 0)
        {
            current = cellStack.Peek();

            List<MazeCell> unvisitedNeighbors = GetUnvisitedNeighbors(current); //방문하지 않은 이웃 찾기

            if (unvisitedNeighbors.Count > 0)
            {
                MazeCell next = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)]; //랜덤하게 이웃 선택
                RemoveWallBetween(current, next);                               //벽 제거
                next.visited = true;
                cellStack.Push(next);
            }
            else
            {
                cellStack.Pop();                                                //백 트래킹
            }
        }
    }


    void CreateCells()                                                          //셀 생성 함수 
    {
        if (cellPrefab == null)
        {
            Debug.LogError("셀 프리팹이 없음");
            return;
        }

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 pos = new Vector3(x * cellSize, 0, z * cellSize);
                GameObject cellObj = Instantiate(cellPrefab, pos, Quaternion.identity, transform);
                cellObj.name = $"Cell_{x}_{z}";

                MazeCell cell = cellObj.GetComponent<MazeCell>();
                if (cell == null)
                {
                    Debug.LogError("MazeCell 스크립트 없음");
                    return;
                }
                cell.Initialize(x, z);
                maze[x, z] = cell;

            }
        }
    }

    List<MazeCell> GetUnvisitedNeighbors(MazeCell cell)                         //방문하지 않은 이웃 찾기 
    {
        List<MazeCell> neighbors = new List<MazeCell>();

        //상하좌우 체크
        if (cell.x > 0 && !maze[cell.x - 1, cell.z].visited)
            neighbors.Add(maze[cell.x - 1, cell.z]);

        if (cell.x < width - 1 && !maze[cell.x + 1, cell.z].visited)
            neighbors.Add(maze[cell.x + 1, cell.z]);

        if (cell.z > 0 && !maze[cell.x, cell.z - 1].visited)
            neighbors.Add(maze[cell.x, cell.z - 1]);

        if (cell.z < height - 1 && !maze[cell.x, cell.z + 1].visited)
            neighbors.Add(maze[cell.x, cell.z + 1]);

        return neighbors;
    }

    void RemoveWallBetween(MazeCell current, MazeCell next)                     //두 셀 사이의 벽 제거
    {
        if (current.x < next.x)                             //오른쪽
        {
            current.RemoveWall("right");
            next.RemoveWall("left");
        }
        else if (current.x > next.x)                        //왼쪽
        {
            current.RemoveWall("left");
            next.RemoveWall("right");
        }
        else if (current.z < next.z)                        //위
        {
            current.RemoveWall("top");
            next.RemoveWall("bottom");
        }
        else if (current.z > next.z)                        //아래
        {
            current.RemoveWall("bottom");
            next.RemoveWall("top");
        }
    }

    //특정 위치의 셀 가져오기
    public MazeCell GetCell(int x, int z)
    {
        if (x >= 0 && x < width && z >= 0 && z < height)
            return maze[x, z];

        return null;
    }

    IEnumerator GenerateWithDFSVisualized()                                     //시각화된 DFS 미로 생성 
    {
        MazeCell current = maze[0, 0];
        current.visited = true;

        current.SetColor(currentColor);                                 //+
        cellStack.Clear();                                              //+

        cellStack.Push(current);                                        //첫번 현재칸을 스택에 넣는다. 

        yield return new WaitForSeconds(viaulizationSpeed);             //+

        int totalCells = width * height;                                //+
        int visitedCount = 1;                                           //+

        while (cellStack.Count > 0)
        {
            current = cellStack.Peek();

            current.SetColor(currentColor);                             //+ (현재 칸 강조)
            yield return new WaitForSeconds(viaulizationSpeed);         //+

            List<MazeCell> unvisitedNeighbors = GetUnvisitedNeighbors(current);                 //방문하지 않은 이웃 찾기

            if (unvisitedNeighbors.Count > 0)
            {
                MazeCell next = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)]; //랜덤하게 이웃 선택
                RemoveWallBetween(current, next);                       //벽 제거

                current.SetColor(visitedColor);                         //+ (현재 칸 방문 완료 색으로 )
                next.visited = true;
                visitedCount++;                                         //+
                cellStack.Push(next);

                next.SetColor(currentColor);
                yield return new WaitForSeconds(viaulizationSpeed);     //+
            }
            else
            {
                current.SetColor(backtrackColor);                       //+
                yield return new WaitForSeconds(viaulizationSpeed);     //+

                current.SetColor(visitedColor);                         //+
                cellStack.Pop();                                        //백 트래킹

            }

            yield return new WaitForSeconds(viaulizationSpeed);         //+
            ResetAllColors();
            Debug.Log($"미로 생성 완료! 총 ({visitedCount} / {totalCells} 칸)");
        }

        void ResetAllColors()                                           //모든 칸 색상 초기화 
        {
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    maze[x, z].SetColor(Color.white);
                }
            }
        }
    }
}