using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Leedong.MergeGame
{
    public class MergeGameHexagon : MergeGame
    {

#region Play

        protected override void Init()
        {
            base.Init();

            _tileDirections = new TileDirection[6] { TileDirection.Up, TileDirection.UpRight, TileDirection.DownRight, TileDirection.Down, TileDirection.DownLeft, TileDirection.UpLeft };
        }

        protected override List<BlockInfo> GetBlockInfos()
        {
            List<BlockInfo> blockInfos = new List<BlockInfo>();

            int count = 0;

            for (int y = 3; y >= -3; y--)
            {
                int a;
                int b;

                switch (y)
                {
                    case -2 :
                    case 2 :
                        a = -1;
                        b = 2;
                        break;
                    case -1 :
                    case 1 :
                        a = -2;
                        b = 2;
                        break;
                    case 0 :
                        a = -2;
                        b = 3;
                        break;                    
                    default : // y == -3 || y == 3
                        a = -1;
                        b = 1;
                        break;
                }

                for (int x = a; x <= b; x++)
                {
                    BlockInfo blockInfo = new BlockInfo()
                    {
                        type = TEST_BLOCK_TYPES[count++],
                        tilePosition = new Vector3Int(x, y, 0)
                    };

                    blockInfos.Add(blockInfo);
                }
            }

            return blockInfos;
        }

        protected override void FindMergeBlocks(Vector3Int position, Block.Type blockType)
        {
            // Find Straight
            base.FindMergeBlocks(position, blockType);
            
            // Find Around
            for (int i = 0; i < _tileDirections.Length; i++)
            {
                Vector3Int blockPoint1 = GetAdjoiningTilePosition(position, _tileDirections[i]);
                
                if (_tilemapBlocks.ContainsKey(blockPoint1) && blockType == _tilemapBlocks[blockPoint1].BlockType)
                {
                    Vector3Int blockPoint2 = GetAdjoiningTilePosition(position, _tileDirections[i + 1 >= _tileDirections.Length ? i + 1 - _tileDirections.Length : i + 1]);

                    if (_tilemapBlocks.ContainsKey(blockPoint2) && blockType == _tilemapBlocks[blockPoint2].BlockType)
                    {
                        Vector3Int blockPoint3 = GetAdjoiningTilePosition(position, _tileDirections[i + 2 >= _tileDirections.Length ? i + 2 - _tileDirections.Length : i + 2]);

                        if (_tilemapBlocks.ContainsKey(blockPoint3) && blockType == _tilemapBlocks[blockPoint3].BlockType)
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

                            if (!_mergePositions.Contains(blockPoint3))
                            {
                                _mergePositions.Add(blockPoint3);
                            }
                        }
                    }
                }
            }
        }

        protected override void MoveBlocks()
        {
            base.MoveBlocks();

            // DownRight or DownLeft, 이동 가능한 블록이 있을 경우 반복
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

                    // point와 targetPoint 둘 다 위에 블록이 없어야 함
                    if (!IsExsitUp(point) && block != null) 
                    {
                        Vector3Int targetPointDR = GetAdjoiningTilePosition(point, TileDirection.DownRight);

                        if (_tilemapBlocks.ContainsKey(targetPointDR))
                        {
                            if (!IsExsitUp(targetPointDR) && _tilemapBlocks[targetPointDR] == null)
                            {
                                _targetPositions.Add(targetPointDR);
                                targetPoint = targetPointDR;
                                isExist = true;
                                break;
                            }
                        }

                        // if no break                                
                        Vector3Int targetPointDL = GetAdjoiningTilePosition(point, TileDirection.DownLeft);

                        if (_tilemapBlocks.ContainsKey(targetPointDL))
                        {
                            if (!IsExsitUp(targetPointDL) && _tilemapBlocks[targetPointDL] == null)
                            {
                                _targetPositions.Add(targetPointDL);
                                targetPoint = targetPointDL;
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

        protected override Vector3Int[] GetDropPosition()
        {
            Vector3Int[] dropPosition = new Vector3Int[1] { new Vector3Int(3, 0, 0) };

            return dropPosition;
        }

        protected override Vector3 GetCellToWorld(Vector3Int tilePosition)
        {
            return _grid.CellToWorld(tilePosition);
        }

#endregion

#region Check

        protected override bool IsAdjoining(Vector3Int tilePositionA, Vector3Int tilePositionB)
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

        protected override Vector3Int GetAdjoiningTilePosition(Vector3Int tilePosition, TileDirection direction)
        {
            switch (direction)
            {
                case TileDirection.Up :
                    return Vector3Int.right * (tilePosition.x + 1) + Vector3Int.up * tilePosition.y;
                case TileDirection.UpRight :
                    return Vector3Int.right * (tilePosition.y % 2 == 0 ? tilePosition.x : tilePosition.x + 1) + Vector3Int.up * (tilePosition.y + 1);
                case TileDirection.DownRight :
                    return Vector3Int.right * (tilePosition.y % 2 == 0 ? tilePosition.x - 1 : tilePosition.x) + Vector3Int.up * (tilePosition.y + 1);
                case TileDirection.Down :
                    return Vector3Int.right * (tilePosition.x - 1) + Vector3Int.up * tilePosition.y;
                case TileDirection.DownLeft :
                    return Vector3Int.right * (tilePosition.y % 2 == 0 ? tilePosition.x - 1 : tilePosition.x) + Vector3Int.up * (tilePosition.y - 1);
                case TileDirection.UpLeft :
                    return Vector3Int.right * (tilePosition.y % 2 == 0 ? tilePosition.x : tilePosition.x + 1) + Vector3Int.up * (tilePosition.y - 1);
                default :
                    return Vector3Int.back;
            }
        }

        // 블록 상단에 다른 블록 존재 체크
        private bool IsExsitUp(Vector3Int tilePosition)
        {
            Vector3Int tilePositionUp = tilePosition;

            bool isExistUp = false;

            while (true)
            {
                tilePositionUp = GetAdjoiningTilePosition(tilePositionUp, TileDirection.Up);

                if (!_tilemapBlocks.ContainsKey(tilePositionUp))
                {
                    break;
                }
                else
                {
                    if (_tilemapBlocks[tilePositionUp] != null)
                    {
                        isExistUp = true;
                        break;
                    }
                }
            }

            return isExistUp;
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

            Block.Type.Pink,
            Block.Type.Green,
            Block.Type.Yellow,
            Block.Type.Green,
            Block.Type.Green,
            Block.Type.Red,

            Block.Type.Blue,
            Block.Type.Red,
            Block.Type.Red,
            Block.Type.Pink,
            Block.Type.Pink,
            Block.Type.Orange,

            Block.Type.Green,
            Block.Type.Blue,
            Block.Type.Orange,
            Block.Type.Yellow,
            Block.Type.Yellow,
            Block.Type.Red,

            Block.Type.Red,
            Block.Type.Orange,
            Block.Type.Green,
            Block.Type.Red,
            Block.Type.Blue,
            Block.Type.Green,

            Block.Type.Yellow,
            Block.Type.Yellow,
            Block.Type.Red,
            Block.Type.Blue,
            Block.Type.Yellow,
            Block.Type.Pink
        };
                
    }
}
