using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Leedong.MergeGame
{
    public class MergeGame : MonoBehaviour
    {
        [Header("Camera")]

        [SerializeField]
        private Camera _camera;

        [Header("Input")]

        [SerializeField]
        private InputHandler _handler;

        [Header("Block")]

        [SerializeField]
        protected Transform _blocks;

        [SerializeField]
        private Sprite _blockRed;

        [SerializeField]
        private Sprite _blockOrange;

        [SerializeField]
        private Sprite _blockYellow;

        [SerializeField]
        private Sprite _blockGreen;

        [SerializeField]
        private Sprite _blockBlue;

        [SerializeField]
        private Sprite _blockPink;

        [Header("Tilemap")]

        [SerializeField]
        protected Grid _grid;

        [SerializeField]
        protected Tilemap _tilemap;

#region Input

        private bool _canInput = true;

        // Input Callback
        private Vector3Int _downTilePosition;

#endregion

#region Play

        protected class BlockInfo
        {
            public Block.Type type;
            public Vector3Int tilePosition;
        }

        protected enum TileDirection
        {
            Up, Down, Left, Right, UpRight, DownRight, DownLeft, UpLeft
        }

        // Directions
        protected TileDirection[] _tileDirections = 
        {
            TileDirection.Up, TileDirection.Down, TileDirection.Left, TileDirection.Right
        };

        private Block.Type[] _blockTypes = 
        {
            Block.Type.Red, Block.Type.Orange, Block.Type.Yellow, Block.Type.Green, Block.Type.Blue, Block.Type.Pink
        };

        // 타일맵의 블록 정보
        protected Dictionary<Vector3Int, Block> _tilemapBlocks = new Dictionary<Vector3Int, Block>();

        // 머지 예정인 블록 리스트
        protected List<Vector3Int> _mergePositions = new List<Vector3Int>();

        // 새 블록들의 위치값 리스트
        protected List<Vector3Int> _targetPositions = new List<Vector3Int>();

        // 블록 컬러
        private Dictionary<Block.Type, Sprite> _blockSprites;

        // 머지 완료된 블록 리스트
        private Queue<Block> _queueBlocks = new Queue<Block>();

#endregion

#region Animation

        private const float ANIM_TIME = 0.25f; // 애니메이션 시간
        private const float ANIM_SPEED_TIME = 4f; // 애니메이션 배속 (speed = ANIM_SPEED_TIME * 이동 거리)

#endregion

        private void Awake()
        {
            Init();
        }

        protected virtual void Init()
        {
            _blockSprites = new Dictionary<Block.Type, Sprite>()
            {
                { Block.Type.Red,    _blockRed },
                { Block.Type.Orange, _blockOrange },
                { Block.Type.Yellow, _blockYellow },
                { Block.Type.Green,  _blockGreen },
                { Block.Type.Blue,   _blockBlue },
                { Block.Type.Pink,   _blockPink },
            };
        }

        private void Start()
        {
            InitHandler();
            InitPlay();
        }

#region Input Callback

        private void InitHandler()
        {
            if (_handler != null)
            {
                _handler.OnInputEvent += InputCallback;
            }
        }

        private void InputCallback(Vector2 screenVec, TouchPhase touchPhase)
        {
            bool isBack = false;

            if (_canInput)
            {
                Vector3Int gridTilePosition = _grid.WorldToCell(_camera.ScreenToWorldPoint(screenVec));

                // 인풋 포지션에 타일이 존재
                if (_tilemap.GetTile(gridTilePosition) != null) 
                {
                    if (touchPhase == TouchPhase.Began)
                    {
                        _downTilePosition = gridTilePosition;
                        Debug.Log("Position : " + _downTilePosition);
                    }
                    else if (touchPhase == TouchPhase.Moved)
                    {
                        if (_downTilePosition == Vector3Int.back)
                        {
                            return;
                        }

                        if (_downTilePosition != gridTilePosition)
                        {
                            // 인접한 타일인지
                            if (IsAdjoining(_downTilePosition, gridTilePosition))
                            {
                                _canInput = false;

                                Vector3Int downTilePosition = _downTilePosition;
                                _downTilePosition = Vector3Int.back;

                                BlockEvent(downTilePosition, gridTilePosition);
                            }
                            else
                            {
                                isBack = true; // 인접하지 않은 타일
                            }
                        }
                    }                    
                    else
                    {
                        isBack = true; // TouchPhase.Ended
                    }
                }
                else
                {
                    isBack = true; // 타일맵 밖 터치
                }

                if (isBack)
                {
                    _downTilePosition = Vector3Int.back;
                }
            }
        }

#endregion

#region Play

        private void InitPlay()
        {
            // 시작 블록
            List<BlockInfo> blockInfos = GetBlockInfos();

            for (int i = 0; i < blockInfos.Count; i++)
            {
                BlockInfo info = blockInfos[i];

                Block block = (Instantiate(Resources.Load("MergeGame/Block"), _blocks.transform) as GameObject).GetComponent<Block>();
                block.SetBlockType(info.type, _blockSprites[info.type]);
                block.transform.position = GetCellToWorld(info.tilePosition);

                _tilemapBlocks.Add(info.tilePosition, block);
            }
        }

        protected virtual List<BlockInfo> GetBlockInfos()
        {
            List<BlockInfo> blockInfos = new List<BlockInfo>();

            int count = 0;

            for (int i = 3; i >= -4; i--)
            {
                for (int j = -4; j <= 3; j++)
                {
                    BlockInfo blockInfo = new BlockInfo()
                    {
                        type = TEST_BLOCK_TYPES[count++],
                        tilePosition = new Vector3Int(i, j, 0)
                    };

                    blockInfos.Add(blockInfo);
                }
            }
            
            return blockInfos;
        }

        private void BlockEvent(Vector3Int downPosition, Vector3Int dragPosition)
        {
            StartCoroutine(StartBlockEvent(downPosition, dragPosition));
        }

        private IEnumerator StartBlockEvent(Vector3Int downPosition, Vector3Int dragPosition)
        {
            _tilemapBlocks[downPosition].SetRendererOrder(3);
            _tilemapBlocks[dragPosition].SetRendererOrder(2);

            BlockChange(downPosition, dragPosition);

            FindMergeBlocks();

            yield return StartCoroutine(StartMoveBoth(_tilemapBlocks[downPosition], _tilemapBlocks[dragPosition]));

            if (_mergePositions.Count > 0)
            {
                yield return StartCoroutine(StartBlockLoopEvent());
            }
            else
            {
                BlockChange(downPosition, dragPosition);

                yield return StartCoroutine(StartMoveBoth(_tilemapBlocks[downPosition], _tilemapBlocks[dragPosition]));
            }

            _canInput = true;
        }

        // 두 블록의 위치값을 서로 변경
        protected void BlockChange(Vector3Int tilePositionA, Vector3Int tilePositionB)
        {
            Block tempBlock = _tilemapBlocks[tilePositionB];
            _tilemapBlocks[tilePositionB] = _tilemapBlocks[tilePositionA];
            _tilemapBlocks[tilePositionA] = tempBlock;
        }

        // 머지 가능한 블록 찾기
        private void FindMergeBlocks()
        {
            _mergePositions.Clear();

            foreach (var item in _tilemapBlocks)
            {
                Vector3Int position = item.Key;
                Block.Type blockType = item.Value.BlockType;

                FindMergeBlocks(position, blockType);
            }
        }

        protected virtual void FindMergeBlocks(Vector3Int position, Block.Type blockType)
        {
            // Find Straight
            if (!_mergePositions.Contains(position))
            {                
                for (int i = 0; i < _tileDirections.Length; i++)
                {
                    Vector3Int blockPoint1 = GetAdjoiningTilePosition(position, _tileDirections[i]);
                    
                    if (_tilemapBlocks.ContainsKey(blockPoint1) && blockType == _tilemapBlocks[blockPoint1].BlockType)
                    {
                        Vector3Int blockPoint2 = GetAdjoiningTilePosition(blockPoint1, _tileDirections[i]);

                        if (_tilemapBlocks.ContainsKey(blockPoint2) && blockType == _tilemapBlocks[blockPoint2].BlockType)
                        {
                            if (!_mergePositions.Contains(position))
                            {
                                _mergePositions.Add(position);
                            }

                            if (!_mergePositions.Contains(blockPoint1))
                            {
                                _mergePositions.Add(blockPoint1);
                            }

                            if (!_mergePositions.Contains(blockPoint2))
                            {
                                _mergePositions.Add(blockPoint2);
                            }
                        }
                    }
                }
            }
        }

#region Loop : Pop -> Create New Blocks & Move -> Find Merge Blocks

        private IEnumerator StartBlockLoopEvent()
        {
            while (true)
            {
                if (_mergePositions.Count == 0)
                {
                    yield break;
                }

#region Pop

                for (int i = 0; i < _mergePositions.Count; i++)
                {
                    Block block = _tilemapBlocks[_mergePositions[i]];

                    _queueBlocks.Enqueue(block);

                    _tilemapBlocks[_mergePositions[i]] = null;
                }

                List<Block> popBlocks = _queueBlocks.ToList();

                yield return StartCoroutine(StartPopBlocks(popBlocks));

#endregion

#region Move & Create New Block

                while (_queueBlocks.Count > 0) 
                {
                    _targetPositions.Clear();

                    MoveBlocks();

                    // Anim Move
                    for (int i = 0; i < _targetPositions.Count; i++)
                    {
                        Vector3 targetPosition = GetCellToWorld(_targetPositions[i]);

                        StartCoroutine(StartMove(_tilemapBlocks[_targetPositions[i]], targetPosition));
                    }

                    // Create New Block
                    Vector3Int[] dropPositions = GetDropPosition();

                    Coroutine[] moveCoroutines = new Coroutine[dropPositions.Length];

                    for (int i = 0; i < dropPositions.Length; i++)
                    {
                        if (_tilemapBlocks[dropPositions[i]] == null)
                        {
                            // Type
                            int randomType = UnityEngine.Random.Range(0, _blockTypes.Length);
                            Sprite sprite = GetBlock(randomType);

                            // Block
                            Block dropBlock = _queueBlocks.Dequeue();
                            dropBlock.SetBlockType(_blockTypes[randomType], sprite);                            
                            dropBlock.gameObject.SetActive(true);

                            // Position
                            Vector3Int dropPosition = dropPositions[i];
                            _tilemapBlocks[dropPosition] = dropBlock;
                            Vector3 creationPosition = GetCellToWorld(dropPosition);
                            dropBlock.transform.position = creationPosition + Vector3.up;

                            moveCoroutines[i] = StartCoroutine(StartMove(dropBlock, creationPosition));
                        }
                    }

                    // 각 코루틴이 종료될 때까지 대기
                    foreach (Coroutine coroutine in moveCoroutines)
                    {
                        if (coroutine != null)
                        {
                            yield return coroutine;
                        }
                    }

                    yield return null;
                }

#endregion

                FindMergeBlocks();
            }
        }

#endregion

        protected virtual void MoveBlocks()
        {
            // Down, 이동 가능한 블록이 있을 경우 반복
            while (true)
            {
                bool isExist = false;

                Vector3Int point = Vector3Int.back;
                Vector3Int targetPoint = Vector3Int.back;

                // _tilemapBlocks은 BlockChange를 통해 계속 갱신, 이동 가능한 블록 한개만 찾음
                foreach (var item in _tilemapBlocks)
                {
                    point = item.Key;

                    // 해당 포인트가 이미 타겟 포인트에 있는 경우 건너뛰기
                    if (_targetPositions.Contains(point)) continue;

                    Block block = item.Value;

                    if (block != null)
                    {
                        targetPoint = GetAdjoiningTilePosition(point, TileDirection.Down);

                        if (_tilemapBlocks.ContainsKey(targetPoint))
                        {
                            if (_tilemapBlocks[targetPoint] == null)
                            {
                                _targetPositions.Add(targetPoint);
                                isExist = true;
                                break;
                            }
                        }
                    }
                }

                if (isExist)
                {
                    BlockChange(point, targetPoint);
                    continue;
                }

                break;
            }
        }

        protected virtual Vector3Int[] GetDropPosition()
        {
            Vector3Int[] dropPositions = new Vector3Int[8];

            for (int i = -4, count = 0; i <= 3; i++, count++)
            {
                dropPositions[count] = new Vector3Int(i, 3, 0);
            }

            return dropPositions;
        }

        protected virtual Vector3 GetCellToWorld(Vector3Int tilePosition)
        {
            return _grid.CellToWorld(tilePosition) + new Vector3(0.5f, 0.5f, 0);
        }

        private Sprite GetBlock(int type)
        {
            switch (type)
            {
                case 1 : 
                    return _blockOrange;
                case 2 : 
                    return _blockYellow;
                case 3 : 
                    return _blockGreen;
                case 4 : 
                    return _blockBlue;
                case 5 : 
                    return _blockPink;
                default :
                    return _blockRed;
            }
        }

#endregion

#region Animation

        // 블록 이동
        private IEnumerator StartMove(Block block, Vector3 targetPosition)
        {
            float distance = Vector3.Distance(block.transform.position, targetPosition);
            float speed = ANIM_SPEED_TIME * distance;

            float timer = 0f;

            while (timer < ANIM_TIME)
            {
                timer += Time.deltaTime;

                block.transform.position = Vector3.MoveTowards(block.transform.position, targetPosition, speed * Time.deltaTime);

                yield return null;
            }

            block.transform.position = targetPosition;
        }

        // 두 블록의 위치를 서로 변경
        private IEnumerator StartMoveBoth(Block downBlock, Block dragBlock)
        {
            Vector3 targetPositionA = downBlock.transform.position;
            Vector3 targetPositionB = dragBlock.transform.position;

            float distance = Vector3.Distance(targetPositionA, targetPositionB);
            float speed = ANIM_SPEED_TIME * distance;

            float timer = 0f;

            while (timer < ANIM_TIME)
            {
                timer += Time.deltaTime;

                downBlock.transform.position = Vector3.MoveTowards(downBlock.transform.position, targetPositionB, speed * Time.deltaTime);
                dragBlock.transform.position = Vector3.MoveTowards(dragBlock.transform.position, targetPositionA, speed * Time.deltaTime);

                yield return null;
            }

            downBlock.transform.position = targetPositionB;
            dragBlock.transform.position = targetPositionA;
        }

        // 블록 제거
        private IEnumerator StartPopBlocks(List<Block> popBlocks)
        {            
            for (int i = 0; i < popBlocks.Count; i++)
            {
                popBlocks[i].Pop();
            }

            float timer = 0f;

            while (timer < ANIM_TIME)
            {
                timer += Time.deltaTime;

                yield return null;
            }

            for (int i = 0; i < popBlocks.Count; i++)
            {
                popBlocks[i].gameObject.SetActive(false);
            }
        }

#endregion

#region Check

        protected virtual bool IsAdjoining(Vector3Int tilePositionA, Vector3Int tilePositionB)
        {
            for (int i = 0; i < _tileDirections.Length; i++)
            {
                if (GetAdjoiningTilePosition(tilePositionA, _tileDirections[i]) == tilePositionB)
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual Vector3Int GetAdjoiningTilePosition(Vector3Int tilePosition, TileDirection direction)
        {
            switch (direction)
            {
                case TileDirection.Up :
                    return tilePosition + Vector3Int.up;
                case TileDirection.Down :
                    return tilePosition + Vector3Int.down;
                case TileDirection.Left :
                    return tilePosition + Vector3Int.left;
                case TileDirection.Right :
                    return tilePosition + Vector3Int.right;
                default :
                    return Vector3Int.back;
            }
        }

#endregion

        private readonly List<Block.Type> TEST_BLOCK_TYPES = new List<Block.Type>()
        {
            Block.Type.Red,
            Block.Type.Blue,
            Block.Type.Blue,
            Block.Type.Red,
            Block.Type.Red,
            Block.Type.Blue,
            Block.Type.Red,
            Block.Type.Pink,

            Block.Type.Pink,
            Block.Type.Green,
            Block.Type.Yellow,
            Block.Type.Green,
            Block.Type.Green,
            Block.Type.Red,
            Block.Type.Yellow,
            Block.Type.Pink,

            Block.Type.Blue,
            Block.Type.Red,
            Block.Type.Red,
            Block.Type.Pink,
            Block.Type.Pink,
            Block.Type.Orange,
            Block.Type.Pink,
            Block.Type.Green,

            Block.Type.Green,
            Block.Type.Blue,
            Block.Type.Orange,
            Block.Type.Green,
            Block.Type.Green,
            Block.Type.Red,
            Block.Type.Yellow,
            Block.Type.Red,

            Block.Type.Blue,
            Block.Type.Orange,
            Block.Type.Green,
            Block.Type.Blue,
            Block.Type.Blue,
            Block.Type.Green,
            Block.Type.Green,
            Block.Type.Red,

            Block.Type.Yellow,
            Block.Type.Yellow,
            Block.Type.Blue,
            Block.Type.Blue,
            Block.Type.Yellow,
            Block.Type.Pink,
            Block.Type.Yellow,
            Block.Type.Blue,

            Block.Type.Yellow,
            Block.Type.Blue,
            Block.Type.Blue,
            Block.Type.Green,
            Block.Type.Pink,
            Block.Type.Blue,
            Block.Type.Red,
            Block.Type.Blue,

            Block.Type.Orange,
            Block.Type.Blue,
            Block.Type.Red,
            Block.Type.Orange,
            Block.Type.Blue,
            Block.Type.Pink,
            Block.Type.Red,
            Block.Type.Pink
        };

    }
}