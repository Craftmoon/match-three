using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class Board : MonoBehaviour
{
    public int width;
    public int height;
    public int borderSize;
    public float swapTime = 0.3f;
    public GameObject tileNormalPrefab;
    public GameObject tileObstaclePrefab;
    public GameObject[] gamePiecePrefabs;       // Array with the dot prefabs
    Tile[,] m_allTiles;                         // Manages the tile positions
    GamePiece[,] m_allGamePieces;               // Manages the position of the actual pieces
    Tile m_clickedTile;
    Tile m_targetTile;
    bool m_playerInputEnabled = true;
    public StartingTile[] startingTiles;

    [System.Serializable]
    public class StartingTile
    {
        public GameObject tilePrefab;
        public int x;
        public int y;
        public int z;
    }

    public void ClickTile(Tile tile)
    {
        if (m_clickedTile == null)
        {
            m_clickedTile = tile;
        }
    }

    public void DragToTile(Tile tile)
    {
        // in the tutorial the " && IsNextTo(m_clickedTile, tile)" was here, 
        // but it wasn't working properly in straigth lines so I moved it to release tile which makes more sense
        if (m_clickedTile != null)
        {
            m_targetTile = tile;
        }
    }

    public void ReleaseTile()
    {
        if (m_clickedTile != null && m_targetTile != null && IsNextTo(m_clickedTile, m_targetTile))
        {
            SwitchTiles(m_clickedTile, m_targetTile);
        }

        m_clickedTile = null;
        m_targetTile = null;

    }

    public void PlaceGamePiece(GamePiece gamePiece, int x, int y)
    {
        if (gamePiece == null)
        {
            Debug.LogWarning("BOARD: Invalid gamePiece.");
            return;
        }

        gamePiece.transform.position = new Vector3(x, y, 0);
        gamePiece.transform.rotation = Quaternion.identity;
        if (isWithinBounds(x, y))
        {
            m_allGamePieces[x, y] = gamePiece;
        }
        gamePiece.SetCoord(x, y);
    }

    void Start()
    // Start is called before the first frame update
    {
        m_allTiles = new Tile[width, height];
        m_allGamePieces = new GamePiece[width, height];

        SetupTiles();
        SetupCamera();
        FillBoard(10, 0.5f);
        //HighlightMatches();
    }

    void SetupTiles()
    // Sets up the tile map in a matrix. The tiles are used as references for position for the gamepieces sometimes.
    {

        foreach (StartingTile sTile in startingTiles)
        {
            if (sTile != null)
            {
                MakeTile(tileObstaclePrefab, sTile.x, sTile.y, sTile.z);
            }
        }
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (m_allTiles[i, j] == null)
                {
                    MakeTile(tileNormalPrefab, i, j);
                }
            }
        }

    }

    void MakeTile(GameObject prefab, int x, int y, int z = 0)
    // Create a single tile
    {
        if (prefab != null)
        {
            //  Instantiate the tile and give it a name
            GameObject tile = Instantiate(prefab, new Vector3(x, y, z), Quaternion.identity) as GameObject;
            tile.name = "Tile (" + x + "," + y + ")";

            // Put the individual tile inside the allTiles matrix
            m_allTiles[x, y] = tile.GetComponent<Tile>();

            // Make each tile have the Board as a parent
            tile.transform.parent = transform;

            m_allTiles[x, y].Init(x, y, this);
        }
    }

    void SetupCamera()
    // Function to set the ortographic size of the camera, so the camera is set up in a way that you can see all the
    // board with some margin to spare in the extremities 
    {
        // The "(float)" before the width/height is because these numbers are initially integers, so they need to be
        // casted as floats, otherwise we lose some data

        // The reason why the width and height are getting subtracted by one is because each style starts at the middle
        // of each tile of the grid. So if the board is 7 tiles wide, it will actually include 8 tiles.
        // So you need to subtact one for the center of the camera to be positioned correctly
        Camera.main.transform.position = new Vector3((float)(width - 1) / 2f, (float)(height - 1) / 2f, -10f);

        float aspectRatio = (float)Screen.width / (float)Screen.height;
        float verticalSize = (float)height / 2f + (float)borderSize;
        float horizontalSize = ((float)width / 2f + (float)borderSize) / aspectRatio;

        Camera.main.orthographicSize = (verticalSize > horizontalSize) ? verticalSize : horizontalSize;
    }

    void FillBoard(int falseYOffset = 0, float moveTime = 0.1f)
    //  Instantiate the gamepieces in the positions of the matrix 
    {
        int maxIterations = 100;
        int iterations = 0;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (m_allGamePieces[i, j] == null && m_allTiles[i, j].tileType != TileType.Obstacle)
                {
                    // Instantiate random gamepiece
                    GamePiece piece = FillRandomAt(i, j, falseYOffset, moveTime);
                    iterations = 0;

                    // Will keep deleting and re-filling the tile with a new GamePiece until there is no match with the previous ones.
                    // Also always put a limit on the while in case it spirals out of the control and freeze the game
                    while (HasMatchOnFill(i, j))
                    {
                        ClearPieceAt(i, j);
                        piece = FillRandomAt(i, j, falseYOffset, moveTime);
                        iterations++;

                        if (iterations >= maxIterations)
                        {
                            Debug.Log("Got caught in FillBoards function's while and looped more than 99 times");
                            break;
                        }

                    }
                }
            }
        }
    }

    // Just to debug/show the groups of gamepieces with more than 3
    void HighlightMatches()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                HighlightMatchesAt(i, j);
            }
        }
    }

    void ClearBoard()
    // Apply Destroy the whole board (apply ClearPieceAt to the whole board)
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                ClearPieceAt(i, j);
            }
        }
    }

    void HighlightPieces(List<GamePiece> gamePieces)
    {
        foreach (GamePiece piece in gamePieces)
        {
            if (piece != null)
            {
                HighlightTileOn(piece.xIndex, piece.yIndex, piece.GetComponent<SpriteRenderer>().color);
            }
        }
    }

    void ClearPieceAt(int x, int y)
    // Destroy the piece in [x,y] position
    {
        GamePiece pieceToClear = m_allGamePieces[x, y];
        if (pieceToClear != null)
        {
            m_allGamePieces[x, y] = null;
            Destroy(pieceToClear.gameObject);
        }

        HighlightTileOff(x, y);
    }

    void ClearPieceAt(List<GamePiece> gamePieces)
    // Destroy all the gamepieces in the list
    {
        foreach (GamePiece piece in gamePieces)
        {
            if (piece != null)
            {
                ClearPieceAt(piece.xIndex, piece.yIndex);
            }
        }
    }

    void SwitchTiles(Tile clickedTile, Tile targetTile)
    // Start SwitchTilesRoutine
    {
        StartCoroutine(SwitchTilesRoutine(clickedTile, targetTile));
    }

    void HighlightTileOff(int x, int y)
    {
        SpriteRenderer spriteRenderer = m_allTiles[x, y].GetComponent<SpriteRenderer>();
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.a, 0);
    }

    void HighlightTileOn(int x, int y, Color col)
    {
        SpriteRenderer spriteRenderer = m_allTiles[x, y].GetComponent<SpriteRenderer>();
        spriteRenderer.color = col;
    }

    void HighlightMatchesAt(int x, int y)
    {
        HighlightTileOff(x, y);

        var combinedMatches = FindMatchesAt(x, y);

        if (combinedMatches.Count > 0)
        {
            foreach (GamePiece piece in combinedMatches)
            {
                HighlightTileOn(piece.xIndex, piece.yIndex, piece.GetComponent<SpriteRenderer>().color);
            }
        }
    }

    void CearAndRefillBoard(List<GamePiece> gamePieces)
    {
        StartCoroutine(ClearAndRefillBoardRoutine(gamePieces));
    }

    bool HasMatchOnFill(int x, int y, int minLength = 3)
    // Check if the game piece in the position [x,y] has any matches
    {
        List<GamePiece> leftMatches = FindMatches(x, y, new Vector2(-1, 0));
        List<GamePiece> downMatches = FindMatches(x, y, new Vector2(0, -1));

        if (leftMatches == null)
        {
            leftMatches = new List<GamePiece>();
        }
        if (downMatches == null)
        {
            downMatches = new List<GamePiece>();
        }

        return (leftMatches.Count > 0 || downMatches.Count > 0);
    }

    bool IsNextTo(Tile start, Tile end)
    {
        if (Mathf.Abs(start.xIndex - end.xIndex) == 1 && end.yIndex == start.yIndex)
        {
            return true;
        }
        if (Mathf.Abs(start.yIndex - end.yIndex) == 1 && end.xIndex == start.xIndex)
        {
            return true;
        }
        return false;
    }

    bool isCollapsed(List<GamePiece> gamePieces)
    {
        foreach (GamePiece piece in gamePieces)
        {
            if (piece != null)
            {
                if (piece.transform.position.y - (float)piece.yIndex > 0.001f)
                {
                    return false;
                }
            }
        }
        return true;
    }

    bool isWithinBounds(int x, int y)
    {
        return (x >= 0 && x < width && y >= 0 && y < height);
    }

    GameObject GetRandomGamePiece()
    {
        int randomIndex = Random.Range(0, gamePiecePrefabs.Length);
        if (gamePiecePrefabs[randomIndex] == null)
        {
            Debug.LogWarning("BOARD: " + randomIndex + " does not contain a valid GamePiece prefab.");
        }
        return gamePiecePrefabs[randomIndex];
    }

    GamePiece FillRandomAt(int x, int y, int falseYOffset = 0, float moveTime = 0.1f)
    {
        GameObject randomPiece = Instantiate(GetRandomGamePiece(), Vector3.zero, Quaternion.identity) as GameObject;
        if (randomPiece != null)
        {
            randomPiece.GetComponent<GamePiece>().Init(this);
            PlaceGamePiece(randomPiece.GetComponent<GamePiece>(), x, y);

            if (falseYOffset != 0)
            {
                randomPiece.transform.position = new Vector3(x, y + falseYOffset, 0);
                randomPiece.GetComponent<GamePiece>().Move(x, y, moveTime);
            }

            randomPiece.transform.parent = transform;
            // We need to get and return the GamePiece here because randomPiece is originally 
            // a GameObject and not a GamePiece which FillBoard's FillRandomAt call requires
            return randomPiece.GetComponent<GamePiece>();
        }
        return null;
    }

    List<GamePiece> FindMatches(int startX, int startY, Vector2 searchDirection, int minLength = 3)
    {
        List<GamePiece> matches = new List<GamePiece>();
        GamePiece startPiece = null;

        if (isWithinBounds(startX, startY))
        {
            startPiece = m_allGamePieces[startX, startY];
        }

        if (startPiece != null)
        {
            matches.Add(startPiece);
        }
        else
        {
            return null;
        }

        int nextX;
        int nextY;

        int maxValue = (width > height) ? width : height;

        for (int i = 1; i < maxValue - 1; i++)
        {
            nextX = startX + (int)Mathf.Clamp(searchDirection.x, -1, 1) * i;
            nextY = startY + (int)Mathf.Clamp(searchDirection.y, -1, 1) * i;

            if (!isWithinBounds(nextX, nextY))
            {
                break;
            }

            GamePiece nextPiece = m_allGamePieces[nextX, nextY];

            // Need to have this check in case there is a hole in the board, or obstacle or something that is null.
            // For example when a piece is deleted and its coordinates set to null
            if (nextPiece == null)
            {
                break;
            }
            else
            {
                if (nextPiece.matchValue == startPiece.matchValue && !matches.Contains(nextPiece))
                {
                    matches.Add(nextPiece);
                }
                else
                {
                    break;
                }
            }
        }

        if (matches.Count >= minLength)
        {
            return matches;
        }

        return null;
    }

    List<GamePiece> FindVerticalMatches(int startX, int startY, int minLength = 3)
    {
        // The minLength here is being passed as 2.
        // It is not 3 because if we have the start gamepiece blue,and the piece above it also blue, 
        // and then the piece below it blue, 3 would send back the two pieces upwards. So it would only match with 5 or more pieces
        List<GamePiece> upwardMatches = FindMatches(startX, startY, new Vector2(0, 1), 2);
        List<GamePiece> downwardMatches = FindMatches(startX, startY, new Vector2(0, -1), 2);

        if (upwardMatches == null)
        {
            upwardMatches = new List<GamePiece>();
        }

        if (downwardMatches == null)
        {
            downwardMatches = new List<GamePiece>();
        }

        var combinedMatches = upwardMatches.Union(downwardMatches).ToList();

        return (combinedMatches.Count >= minLength ? combinedMatches : null);
    }

    List<GamePiece> FindHorizontalMatches(int startX, int startY, int minLength = 3)
    {
        // The minLength here is being passed as 2.
        // It is not 3 because if we have the start gamepiece blue,and the piece above it also blue, 
        // and then the piece below it blue, 3 would send back the two pieces upwards. So it would only match with 5 or more pieces
        List<GamePiece> rightMatches = FindMatches(startX, startY, new Vector2(1, 0), 2);
        List<GamePiece> leftMatches = FindMatches(startX, startY, new Vector2(-1, 0), 2);

        if (rightMatches == null)
        {
            rightMatches = new List<GamePiece>();
        }

        if (leftMatches == null)
        {
            leftMatches = new List<GamePiece>();
        }

        var combinedMatches = rightMatches.Union(leftMatches).ToList();

        return (combinedMatches.Count >= minLength ? combinedMatches : null);
    }

    List<GamePiece> FindMatchesAt(int x, int y, int minLength = 3)
    {
        List<GamePiece> verticalMatches = FindVerticalMatches(x, y, minLength);
        List<GamePiece> horizontalMatches = FindHorizontalMatches(x, y, minLength);

        if (verticalMatches == null)
        {
            verticalMatches = new List<GamePiece>();
        }

        if (horizontalMatches == null)
        {
            horizontalMatches = new List<GamePiece>();
        }

        var combinedMatches = verticalMatches.Union(horizontalMatches).ToList();

        return combinedMatches;
    }

    List<GamePiece> FindAllMatches()
    {
        List<GamePiece> combinedMatches = new List<GamePiece>();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                //MIGHT NEED TO CHECK IF THE GAMEPIECE ISN'T ALREADY IN THE COMBINED MATCHES IF SOMETHING GOES WRONG IN THE FUTURE
                List<GamePiece> matches = FindMatchesAt(i, j);
                combinedMatches = combinedMatches.Union(matches).ToList();
            }
        }
        return combinedMatches;
    }

    List<GamePiece> FindMatchesAt(List<GamePiece> gamePieces, int minLength = 3)
    {
        List<GamePiece> combinedMatches = new List<GamePiece>();

        // The way the professor did was like this but I think the way I did it is more safe
        // foreach (GamePiece piece in gamePieces)
        // {
        //     combinedMatches = combinedMatches.Union(FindMatchesAt(piece.xIndex, piece.yIndex, minLength)).ToList();
        // }

        foreach (GamePiece piece in gamePieces)
        {
            List<GamePiece> foundMatches = FindMatchesAt(piece.xIndex, piece.yIndex, minLength);

            foreach (GamePiece foundPiece in foundMatches)
            {
                if (!combinedMatches.Contains(foundPiece))
                {
                    combinedMatches.Add(foundPiece);
                }
            }
        }

        return combinedMatches;
    }

    List<GamePiece> CollapseColumn(int column, float collapseTime = 0.1f)
    {
        List<GamePiece> movingPieces = new List<GamePiece>();

        for (int i = 0; i < height - 1; i++)
        {
            if (m_allGamePieces[column, i] == null && m_allTiles[column, i].tileType != TileType.Obstacle)
            {
                for (int j = i + 1; j < height; j++)
                {
                    if (m_allGamePieces[column, j] != null)
                    {
                        m_allGamePieces[column, j].Move(column, i, collapseTime * (j - i));

                        // Move already does this, but since it takes some time we're doing it here faster
                        // So situations like "piece is moving so the place it is moving to still is null" doesn't happen
                        m_allGamePieces[column, i] = m_allGamePieces[column, j];
                        m_allGamePieces[column, i].SetCoord(column, i);

                        if (!movingPieces.Contains(m_allGamePieces[column, i]))
                        {
                            movingPieces.Add(m_allGamePieces[column, i]);
                        }

                        m_allGamePieces[column, j] = null;
                        break;
                    }
                }
            }
        }
        return movingPieces;
    }

    List<GamePiece> CollapseColumn(List<GamePiece> gamePieces)
    {
        List<int> columnsList = GetColumns(gamePieces);
        List<GamePiece> movingPieces = new List<GamePiece>();

        foreach (int column in columnsList)
        {
            movingPieces = movingPieces.Union(CollapseColumn(column)).ToList();
        }

        return movingPieces;
    }

    List<int> GetColumns(List<GamePiece> GamePieces)
    {
        List<int> columns = new List<int>();

        foreach (GamePiece piece in GamePieces)
        {
            if (!columns.Contains(piece.xIndex))
            {
                columns.Add(piece.xIndex);
            }
        }

        return columns;
    }

    IEnumerator ClearAndRefillBoardRoutine(List<GamePiece> gamePieces)
    {
        m_playerInputEnabled = false;

        List<GamePiece> matches = gamePieces;

        do
        {
            // Clear and collapse 
            yield return StartCoroutine(ClearAndCollapseRoutine(matches));
            yield return null;


            // Refill
            StartCoroutine(FillBoardRoutine());
            matches = FindAllMatches();

            yield return new WaitForSeconds(0.25f);
        }
        while (matches.Count != 0);
        m_playerInputEnabled = true;

    }

    IEnumerator ClearAndCollapseRoutine(List<GamePiece> gamePieces)
    {

        List<GamePiece> movingPieces = new List<GamePiece>();
        List<GamePiece> matches = new List<GamePiece>();

        HighlightPieces(gamePieces);
        yield return new WaitForSeconds(0.5f);
        bool isFinished = false;

        while (!isFinished)
        {
            ClearPieceAt(gamePieces);

            yield return new WaitForSeconds(0.25f);

            movingPieces = CollapseColumn(gamePieces);
            while (!isCollapsed(movingPieces))
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.25f);

            matches = FindMatchesAt(movingPieces);

            if (matches.Count == 0)
            {
                isFinished = true;
                break;
            }
            else
            {
                yield return StartCoroutine(ClearAndCollapseRoutine(matches));
            }
        }
        yield return null;
    }

    IEnumerator FillBoardRoutine()
    {
        FillBoard(10, 0.5f);
        yield return null;
    }

    IEnumerator SwitchTilesRoutine(Tile clickedTile, Tile targetTile)
    {
        // This if here isn't probably the best way to deal with it but I'll follow the professor for now
        if (m_playerInputEnabled)
        {
            GamePiece clickedPiece = m_allGamePieces[clickedTile.xIndex, clickedTile.yIndex];
            GamePiece targetPiece = m_allGamePieces[targetTile.xIndex, targetTile.yIndex];

            if (clickedPiece != null && targetPiece != null)
            {
                clickedPiece.Move(targetTile.xIndex, targetTile.yIndex, swapTime);
                targetPiece.Move(clickedTile.xIndex, clickedTile.yIndex, swapTime);

                yield return new WaitForSeconds(swapTime);

                List<GamePiece> clickedPieceMatches = FindMatchesAt(clickedPiece.xIndex, clickedPiece.yIndex);
                List<GamePiece> targetPieceMatches = FindMatchesAt(targetPiece.xIndex, targetPiece.yIndex);

                if (clickedPieceMatches.Count == 0 && targetPieceMatches.Count == 0)
                {
                    // This clickedPiece.Move above (not below) doesn't change the clickedPiece x and y Indexes
                    // That is way the parameters being used for Move below are the same parameters of the objects being moved
                    // as opposed of the parameters of the other object
                    clickedPiece.Move(clickedTile.xIndex, clickedTile.yIndex, swapTime);
                    targetPiece.Move(targetTile.xIndex, targetTile.yIndex, swapTime);
                }
                else
                {
                    // yield return new WaitForSeconds(swapTime);

                    // This here wouldn't work if not in a coroutine because when this is called, the coroutine of move hasn't finished yet
                    // In the coroutine the gamepiece is placed at it's place only when reachedDestination = true;
                    // So when you try to highlight the clicked piece, it will think the clicked piece is where it started instead of where
                    // It should e when the switch movement is complete.
                    // HighlightMatchesAt(clickedPiece.xIndex, clickedPiece.yIndex);
                    // HighlightMatchesAt(targetPiece.xIndex, targetPiece.yIndex);

                    // ClearPieceAt(clickedPieceMatches);
                    // ClearPieceAt(targetPieceMatches);
                    // CollapseColumn(clickedPieceMatches.Union(targetPieceMatches).ToList());

                    CearAndRefillBoard(clickedPieceMatches.Union(targetPieceMatches).ToList());

                }

            }
        }

    }

}
