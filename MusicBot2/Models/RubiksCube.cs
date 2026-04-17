using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicBot2.Models
{
    /// <summary>
    /// 3x3 魔術方塊模型
    /// </summary>
    public class RubiksCube
    {
        // 六個面: Front, Back, Up, Down, Left, Right
        private char[,] F = new char[3, 3]; // 前
        private char[,] B = new char[3, 3]; // 後
        private char[,] U = new char[3, 3]; // 上
        private char[,] D = new char[3, 3]; // 下
        private char[,] L = new char[3, 3]; // 左
        private char[,] R = new char[3, 3]; // 右

        public int MoveCount { get; private set; }

        // 顏色表示
        private const char WHITE = 'W';
        private const char YELLOW = 'Y';
        private const char RED = 'R';
        private const char ORANGE = 'O';
        private const char GREEN = 'G';
        private const char BLUE = 'B';

        public RubiksCube()
        {
            InitializeSolved();
        }

        /// <summary>
        /// 初始化為完成狀態
        /// </summary>
        private void InitializeSolved()
        {
            FillFace(F, WHITE);
            FillFace(B, YELLOW);
            FillFace(U, GREEN);
            FillFace(D, BLUE);
            FillFace(L, ORANGE);
            FillFace(R, RED);
            MoveCount = 0;
        }

        private void FillFace(char[,] face, char color)
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    face[i, j] = color;
        }

        /// <summary>
        /// 旋轉指定面
        /// </summary>
        public void Rotate(string face, bool clockwise)
        {
            switch (face.ToUpper())
            {
                case "F": RotateFront(clockwise); break;
                case "B": RotateBack(clockwise); break;
                case "U": RotateUp(clockwise); break;
                case "D": RotateDown(clockwise); break;
                case "L": RotateLeft(clockwise); break;
                case "R": RotateRight(clockwise); break;
            }
            MoveCount++;
        }

        /// <summary>
        /// 旋轉前面 (F)
        /// </summary>
        private void RotateFront(bool clockwise)
        {
            RotateFaceItself(F, clockwise);

            if (clockwise)
            {
                // 保存 U 的底行
                char[] temp = new char[3];
                for (int i = 0; i < 3; i++) temp[i] = U[2, i];

                // U底 <- L右
                for (int i = 0; i < 3; i++) U[2, i] = L[2 - i, 2];

                // L右 <- D頂
                for (int i = 0; i < 3; i++) L[i, 2] = D[0, i];

                // D頂 <- R左
                for (int i = 0; i < 3; i++) D[0, i] = R[2 - i, 0];

                // R左 <- temp
                for (int i = 0; i < 3; i++) R[i, 0] = temp[i];
            }
            else
            {
                // 逆時針
                char[] temp = new char[3];
                for (int i = 0; i < 3; i++) temp[i] = U[2, i];

                for (int i = 0; i < 3; i++) U[2, i] = R[i, 0];
                for (int i = 0; i < 3; i++) R[i, 0] = D[0, 2 - i];
                for (int i = 0; i < 3; i++) D[0, i] = L[i, 2];
                for (int i = 0; i < 3; i++) L[i, 2] = temp[2 - i];
            }
        }

        /// <summary>
        /// 旋轉後面 (B)
        /// </summary>
        private void RotateBack(bool clockwise)
        {
            RotateFaceItself(B, clockwise);

            if (clockwise)
            {
                char[] temp = new char[3];
                for (int i = 0; i < 3; i++) temp[i] = U[0, i];

                for (int i = 0; i < 3; i++) U[0, i] = R[i, 2];
                for (int i = 0; i < 3; i++) R[i, 2] = D[2, 2 - i];
                for (int i = 0; i < 3; i++) D[2, i] = L[i, 0];
                for (int i = 0; i < 3; i++) L[i, 0] = temp[2 - i];
            }
            else
            {
                char[] temp = new char[3];
                for (int i = 0; i < 3; i++) temp[i] = U[0, i];

                for (int i = 0; i < 3; i++) U[0, i] = L[2 - i, 0];
                for (int i = 0; i < 3; i++) L[i, 0] = D[2, i];
                for (int i = 0; i < 3; i++) D[2, i] = R[2 - i, 2];
                for (int i = 0; i < 3; i++) R[i, 2] = temp[i];
            }
        }

        /// <summary>
        /// 旋轉上面 (U)
        /// </summary>
        private void RotateUp(bool clockwise)
        {
            RotateFaceItself(U, clockwise);

            if (clockwise)
            {
                char[] temp = new char[3];
                for (int i = 0; i < 3; i++) temp[i] = F[0, i];

                for (int i = 0; i < 3; i++) F[0, i] = R[0, i];
                for (int i = 0; i < 3; i++) R[0, i] = B[0, i];
                for (int i = 0; i < 3; i++) B[0, i] = L[0, i];
                for (int i = 0; i < 3; i++) L[0, i] = temp[i];
            }
            else
            {
                char[] temp = new char[3];
                for (int i = 0; i < 3; i++) temp[i] = F[0, i];

                for (int i = 0; i < 3; i++) F[0, i] = L[0, i];
                for (int i = 0; i < 3; i++) L[0, i] = B[0, i];
                for (int i = 0; i < 3; i++) B[0, i] = R[0, i];
                for (int i = 0; i < 3; i++) R[0, i] = temp[i];
            }
        }

        /// <summary>
        /// 旋轉下面 (D)
        /// </summary>
        private void RotateDown(bool clockwise)
        {
            RotateFaceItself(D, clockwise);

            if (clockwise)
            {
                char[] temp = new char[3];
                for (int i = 0; i < 3; i++) temp[i] = F[2, i];

                for (int i = 0; i < 3; i++) F[2, i] = L[2, i];
                for (int i = 0; i < 3; i++) L[2, i] = B[2, i];
                for (int i = 0; i < 3; i++) B[2, i] = R[2, i];
                for (int i = 0; i < 3; i++) R[2, i] = temp[i];
            }
            else
            {
                char[] temp = new char[3];
                for (int i = 0; i < 3; i++) temp[i] = F[2, i];

                for (int i = 0; i < 3; i++) F[2, i] = R[2, i];
                for (int i = 0; i < 3; i++) R[2, i] = B[2, i];
                for (int i = 0; i < 3; i++) B[2, i] = L[2, i];
                for (int i = 0; i < 3; i++) L[2, i] = temp[i];
            }
        }

        /// <summary>
        /// 旋轉左面 (L)
        /// </summary>
        private void RotateLeft(bool clockwise)
        {
            RotateFaceItself(L, clockwise);

            if (clockwise)
            {
                char[] temp = new char[3];
                for (int i = 0; i < 3; i++) temp[i] = F[i, 0];

                for (int i = 0; i < 3; i++) F[i, 0] = U[i, 0];
                for (int i = 0; i < 3; i++) U[i, 0] = B[2 - i, 2];
                for (int i = 0; i < 3; i++) B[i, 2] = D[2 - i, 0];
                for (int i = 0; i < 3; i++) D[i, 0] = temp[i];
            }
            else
            {
                char[] temp = new char[3];
                for (int i = 0; i < 3; i++) temp[i] = F[i, 0];

                for (int i = 0; i < 3; i++) F[i, 0] = D[i, 0];
                for (int i = 0; i < 3; i++) D[i, 0] = B[2 - i, 2];
                for (int i = 0; i < 3; i++) B[i, 2] = U[2 - i, 0];
                for (int i = 0; i < 3; i++) U[i, 0] = temp[i];
            }
        }

        /// <summary>
        /// 旋轉右面 (R)
        /// </summary>
        private void RotateRight(bool clockwise)
        {
            RotateFaceItself(R, clockwise);

            if (clockwise)
            {
                char[] temp = new char[3];
                for (int i = 0; i < 3; i++) temp[i] = F[i, 2];

                for (int i = 0; i < 3; i++) F[i, 2] = D[i, 2];
                for (int i = 0; i < 3; i++) D[i, 2] = B[2 - i, 0];
                for (int i = 0; i < 3; i++) B[i, 0] = U[2 - i, 2];
                for (int i = 0; i < 3; i++) U[i, 2] = temp[i];
            }
            else
            {
                char[] temp = new char[3];
                for (int i = 0; i < 3; i++) temp[i] = F[i, 2];

                for (int i = 0; i < 3; i++) F[i, 2] = U[i, 2];
                for (int i = 0; i < 3; i++) U[i, 2] = B[2 - i, 0];
                for (int i = 0; i < 3; i++) B[i, 0] = D[2 - i, 2];
                for (int i = 0; i < 3; i++) D[i, 2] = temp[i];
            }
        }

        /// <summary>
        /// 旋轉面本身 90度
        /// </summary>
        private void RotateFaceItself(char[,] face, bool clockwise)
        {
            char[,] temp = new char[3, 3];
            Array.Copy(face, temp, 9);

            if (clockwise)
            {
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                        face[i, j] = temp[2 - j, i];
            }
            else
            {
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                        face[i, j] = temp[j, 2 - i];
            }
        }

        /// <summary>
        /// 打亂魔術方塊
        /// </summary>
        public void Scramble(int moves)
        {
            var random = new Random();
            var faces = new[] { "F", "B", "U", "D", "L", "R" };

            for (int i = 0; i < moves; i++)
            {
                string face = faces[random.Next(faces.Length)];
                bool clockwise = random.Next(2) == 0;
                Rotate(face, clockwise);
            }

            // 重置步數計數
            MoveCount = 0;
        }

        /// <summary>
        /// 檢查是否完成
        /// </summary>
        public bool IsSolved()
        {
            return IsFaceSolved(F) && IsFaceSolved(B) &&
                   IsFaceSolved(U) && IsFaceSolved(D) &&
                   IsFaceSolved(L) && IsFaceSolved(R);
        }

        private bool IsFaceSolved(char[,] face)
        {
            char first = face[0, 0];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    if (face[i, j] != first)
                        return false;
            return true;
        }

        /// <summary>
        /// 獲取視覺化字串（直式排列）
        /// </summary>
        public string GetVisualRepresentation()
        {
            var sb = new StringBuilder();

            // 轉換顏色為 Emoji
            Func<char, string> getEmoji = (c) => c switch
            {
                'W' => "⬜", // 白
                'Y' => "🟨", // 黃
                'R' => "🟥", // 紅
                'O' => "🟧", // 橘
                'G' => "🟩", // 綠
                'B' => "🟦", // 藍
                _ => "⬛"
            };

            // 直式排列：從上到下顯示 U -> F -> D -> B
            sb.AppendLine("       ┌─────────┐");
            sb.AppendLine("       │  上 (U) │");
            sb.AppendLine("       └─────────┘");
            
            for (int i = 0; i < 3; i++)
            {
                sb.Append("       ");
                for (int j = 0; j < 3; j++)
                    sb.Append(getEmoji(U[i, j]));
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("┌──────┬─────────┬──────┐");
            sb.AppendLine("│左 (L)│  前 (F) │右 (R)│");
            sb.AppendLine("└──────┴─────────┴──────┘");

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++) sb.Append(getEmoji(L[i, j]));
                sb.Append(" ");
                for (int j = 0; j < 3; j++) sb.Append(getEmoji(F[i, j]));
                sb.Append(" ");
                for (int j = 0; j < 3; j++) sb.Append(getEmoji(R[i, j]));
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("       ┌─────────┐");
            sb.AppendLine("       │  下 (D) │");
            sb.AppendLine("       └─────────┘");

            for (int i = 0; i < 3; i++)
            {
                sb.Append("       ");
                for (int j = 0; j < 3; j++)
                    sb.Append(getEmoji(D[i, j]));
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("       ┌─────────┐");
            sb.AppendLine("       │  後 (B) │");
            sb.AppendLine("       └─────────┘");

            for (int i = 0; i < 3; i++)
            {
                sb.Append("       ");
                for (int j = 0; j < 3; j++)
                    sb.Append(getEmoji(B[i, j]));
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}