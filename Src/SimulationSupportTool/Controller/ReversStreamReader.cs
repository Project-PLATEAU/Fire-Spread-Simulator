using System.IO;
using System.Text;

namespace SimulationSupportTool.Controller
{
    /// <summary>
    /// 末尾から1行ずつテキストを取得するファイル読み込みクラス
    /// </summary>
    public class ReversStreamReader : StreamReader
    {
        private int peekIndex = 0;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="stream">読み取るストリーム</param>
        public ReversStreamReader(Stream stream)
            : base(stream)
        {
            this.BaseStream.Position = this.BaseStream.Seek(0, SeekOrigin.End);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="path">読み込まれる完全なファイルパス</param>
        public ReversStreamReader(string path)
            : base(path)
        {
            this.BaseStream.Position = this.BaseStream.Seek(0, SeekOrigin.End);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="stream">読み取るストリーム</param>
        /// <param name="detectEncodingFromByteOrderMarks">ファイルの先頭にあるバイト順序マークを検索するかどうかを示す</param>
        public ReversStreamReader(Stream stream, bool detectEncodingFromByteOrderMarks)
            : base(stream, detectEncodingFromByteOrderMarks)
        {
            this.BaseStream.Position = this.BaseStream.Seek(0, SeekOrigin.End);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="stream">読み取るストリーム</param>
        /// <param name="encoding">使用する文字エンコーディング</param>
        public ReversStreamReader(Stream stream, Encoding encoding)
            : base(stream, encoding)
        {
            this.BaseStream.Position = this.BaseStream.Seek(0, SeekOrigin.End);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="path">読み込まれる完全なファイルパス</param>
        /// <param name="detectEncodingFromByteOrderMarks">ファイルの先頭にあるバイト順序マークを検索するかどうかを示す</param>
        public ReversStreamReader(string path, bool detectEncodingFromByteOrderMarks)
            : base(path, detectEncodingFromByteOrderMarks)
        {
            this.BaseStream.Position = this.BaseStream.Seek(0, SeekOrigin.End);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="path">読み込まれる完全なファイルパス</param>
        /// <param name="options">基になるFileStreamの構成オプションを指定する オブジェクト</param>
        public ReversStreamReader(string path, FileStreamOptions options)
            : base(path, options)
        {
            this.BaseStream.Position = this.BaseStream.Seek(0, SeekOrigin.End);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="path">読み込まれる完全なファイルパス</param>
        /// <param name="encoding">使用する文字エンコーディング</param>
        public ReversStreamReader(string path, Encoding encoding)
            : base(path, encoding)
        {
            this.BaseStream.Position = this.BaseStream.Seek(0, SeekOrigin.End);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="stream">読み取るストリーム</param>
        /// <param name="encoding">使用する文字エンコーディング</param>
        /// <param name="detectEncodingFromByteOrderMarks">ファイルの先頭にあるバイト順序マークを検索するかどうかを示す</param>
        public ReversStreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks)
            : base(stream, encoding, detectEncodingFromByteOrderMarks)
        {
            this.BaseStream.Position = this.BaseStream.Seek(0, SeekOrigin.End);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="path">読み込まれる完全なファイルパス</param>
        /// <param name="encoding">使用する文字エンコーディング</param>
        /// <param name="detectEncodingFromByteOrderMarks">ファイルの先頭にあるバイト順序マークを検索するかどうかを示す</param>
        public ReversStreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks)
            : base(path, encoding, detectEncodingFromByteOrderMarks)
        {
            this.BaseStream.Position = this.BaseStream.Seek(0, SeekOrigin.End);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="stream">読み取るストリーム</param>
        /// <param name="encoding">使用する文字エンコーディング</param>
        /// <param name="detectEncodingFromByteOrderMarks">ファイルの先頭にあるバイト順序マークを検索するかどうかを示す</param>
        /// <param name="bufferSize">最小バッファーサイズ（単位は16 ビット文字数）</param>
        public ReversStreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
            : base(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize)
        {
            this.BaseStream.Position = this.BaseStream.Seek(0, SeekOrigin.End);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="path">読み込まれる完全なファイルパス</param>
        /// <param name="encoding">使用する文字エンコーディング</param>
        /// <param name="detectEncodingFromByteOrderMarks">ファイルの先頭にあるバイト順序マークを検索するかどうかを示す</param>
        /// <param name="bufferSize">最小バッファーサイズ（単位は16 ビット文字数）</param>
        public ReversStreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
            : base(path, encoding, detectEncodingFromByteOrderMarks, bufferSize)
        {
            this.BaseStream.Position = this.BaseStream.Seek(0, SeekOrigin.End);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="path">読み込まれる完全なファイルパス</param>
        /// <param name="encoding">使用する文字エンコーディング</param>
        /// <param name="detectEncodingFromByteOrderMarks">ファイルの先頭にあるバイト順序マークを検索するかどうかを示す</param>
        /// <param name="options">基になるFileStreamの構成オプションを指定する オブジェクト</param>
        public ReversStreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, FileStreamOptions options)
            : base(path, encoding, detectEncodingFromByteOrderMarks, options)
        {
            this.BaseStream.Position = this.BaseStream.Seek(0, SeekOrigin.End);
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="stream">読み取るストリーム</param>
        /// <param name="encoding">使用する文字エンコーディング</param>
        /// <param name="detectEncodingFromByteOrderMarks">ファイルの先頭にあるバイト順序マークを検索するかどうかを示す</param>
        /// <param name="bufferSize">最小バッファーサイズ（単位は16 ビット文字数）</param>
        /// <param name="leaveOpen">コンストラクタ終了後も <paramref name="stream"/> を開いたままにするかどうかを示す（true の場合はストリームを閉じない）</param>
        public ReversStreamReader(Stream stream, Encoding? encoding = null, bool detectEncodingFromByteOrderMarks = true, int bufferSize = -1, bool leaveOpen = false)
            : base(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen)
        {
            this.BaseStream.Position = this.BaseStream.Seek(0, SeekOrigin.End);
        }

        /// <inheritdoc/>
        public override int Peek()
        {
            return this.peekIndex;
        }

        /// <inheritdoc/>
        public override string ReadLine()
        {
            const int bufferSize = 4096;
            var lineText = string.Empty;
            var crIndex = -1;
            var lfIndex = -1;
            long buffaLength;
            long startPosition;
            byte[] crByte = this.CurrentEncoding.GetBytes("\r");
            byte[] lfByte = this.CurrentEncoding.GetBytes("\n");

            while (true)
            {
                if (this.BaseStream.Position == 0)
                {
                    // 先頭が改行コードの場合
                    this.peekIndex = -1;
                    return lineText;
                }
                else if (this.BaseStream.Position < bufferSize)
                {
                    // バッファサイズ調整
                    buffaLength = this.BaseStream.Position;
                    this.BaseStream.Position = 0;
                }
                else
                {
                    buffaLength = bufferSize;
                    this.BaseStream.Position -= bufferSize;
                }

                if (!this.BaseStream.CanSeek)
                {
                    // 念のため
                    return lineText;
                }

                // バッファサイズ分読み取る前に、初期ポジションを取得
                startPosition = this.BaseStream.Position;
                byte[] bytes = new byte[buffaLength];

                // 取得Byteを1Byteずつ配列格納
                for (int index = 0; index < bytes.GetLength(0); index++)
                {
                    int read = this.BaseStream.ReadByte();

                    // 改行コードの最終ポジションを記憶する
                    if (crByte[0] == (byte)read)
                    {
                        crIndex = index; // CR
                    }
                    else if (lfByte[0] == (byte)read)
                    {
                        lfIndex = index; // LF
                    }

                    bytes[index] = (byte)read;
                }

                // 取得Byte1行分を文字列変換
                if (crIndex >= 0 && lfIndex >= 0 && crIndex == lfIndex - 1)
                {
                    // CRLF
                    byte[] copys = new byte[bytes.GetLength(0) - (lfIndex + 1)];
                    Array.Copy(bytes, lfIndex + 1, copys, 0, copys.GetLength(0));
                    lineText = this.CurrentEncoding.GetString(copys) + lineText;
                    this.BaseStream.Position = startPosition + crIndex;
                    return lineText;
                }
                else if (crIndex >= 0 && lfIndex < crIndex)
                {
                    // CR
                    byte[] copys = new byte[bytes.GetLength(0) - (crIndex + 1)];
                    Array.Copy(bytes, crIndex + 1, copys, 0, copys.GetLength(0));
                    lineText = this.CurrentEncoding.GetString(copys) + lineText;
                    this.BaseStream.Position = startPosition + crIndex;
                    return lineText;
                }
                else if (lfIndex >= 0 && lfIndex > crIndex)
                {
                    // LF
                    byte[] copys = new byte[bytes.GetLength(0) - (lfIndex + 1)];
                    Array.Copy(bytes, lfIndex + 1, copys, 0, copys.GetLength(0));
                    lineText = this.CurrentEncoding.GetString(copys) + lineText;
                    this.BaseStream.Position = startPosition + lfIndex;
                    return lineText;
                }
                else
                {
                    // 改行コードなし
                    lineText = this.CurrentEncoding.GetString(bytes) + lineText;
                    this.BaseStream.Position = startPosition;
                }
            }
        }
    }
}
