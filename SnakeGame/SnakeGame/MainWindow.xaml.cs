using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace SnakeGame
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 组成蛇身体的方块的大小
        /// </summary>
        const int SnakeSquareSize = 20;
        /// <summary>
        /// 蛇身体的颜色
        /// </summary>
        private SolidColorBrush snakeBodyBrush = Brushes.Green;
        /// <summary>
        /// 蛇头的颜色
        /// </summary>
        private SolidColorBrush snakeHeadBrush = Brushes.Blue;
        /// <summary>
        /// 蛇身体的各个部分
        /// </summary>
        private List<SnakePart> snakeParts = new List<SnakePart>();
        /// <summary>
        /// 蛇的爬行方向
        /// </summary>
        private SnakeDirection snakeDirection = SnakeDirection.Right;
        /// <summary>
        /// 蛇的长度
        /// </summary>
        private int snakeLength;
        /// <summary>
        /// 让蛇前进的定时器
        /// </summary>
        private DispatcherTimer gameTickTimer = new DispatcherTimer();
        /// <summary>
        /// 蛇开始的长度
        /// </summary>
        const int SnakeStartLength = 3;
        /// <summary>
        /// 蛇开始的速度
        /// </summary>
        const int SnakeStartSpeed = 400;
        /// <summary>
        /// 每一步，最少的时间，毫秒数
        /// </summary>
        const int SnakeSpeedThreshold = 100;
        /// <summary>
        /// 产生食物位置的随机数
        /// </summary>
        private Random rnd = new Random();
        /// <summary>
        /// 蛇的食物控件
        /// </summary>
        private UIElement snakeFood = null;
        /// <summary>
        /// 食物的颜色
        /// </summary>
        private SolidColorBrush foodBrush = Brushes.Red;
        /// <summary>
        /// 当前的分数
        /// </summary>
        private int currentScore = 0;
        /// <summary>
        /// 高分榜的中的高分数量
        /// </summary>
        const int MaxHighscoreListEntryCount = 5;
        /// <summary>
        /// 文本转语音
        /// </summary>
        private SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer();
        /// <summary>
        /// 高分列表
        /// </summary>
        public ObservableCollection<SnakeHighscore> HighscoreList { get => highscoreList; set => highscoreList = value; }
        private ObservableCollection<SnakeHighscore> highscoreList = new ObservableCollection<SnakeHighscore>();
        /// <summary>
        /// 高分列表文件名
        /// </summary>
        const string snake_highscorelist_file = "snake_highscorelist.xml";
        /// <summary>
        /// 游戏是否在运行中
        /// </summary>
        Boolean isGameRunning = false;

        public MainWindow()
        {
            InitializeComponent();
            //计时器事件绑定
            gameTickTimer.Tick += GameTickTimer_Tick;
            //加载高分列表
            LoadHighscoreList();

        }
        /// <summary>
        /// 在窗口的内容呈现完毕之后发生
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DrawGameArea();
        }
       
        /// <summary>
        /// 创建新食物
        /// </summary>
        private void DrawSnakeFood()
        {
            //取一个可以放食物的坐标
            Point foodPosition = GetNextFoodPosition();
            //创建一个新食物
            snakeFood = new Ellipse()
            {
                Width = SnakeSquareSize,
                Height = SnakeSquareSize,
                Fill = foodBrush
            };
            //添加到游戏棋盘中
            GameArea.Children.Add(snakeFood);

            //设置食物在棋盘中的位置
            Canvas.SetTop(snakeFood, foodPosition.Y);
            Canvas.SetLeft(snakeFood, foodPosition.X);
        }

        /// <summary>
        /// 开始新游戏
        /// </summary>
        private void StartNewGame()
        {
            //欢迎，隐藏
            bdrWelcomeMessage.Visibility = Visibility.Collapsed;
            //高分，隐藏
            bdrHighscoreList.Visibility = Visibility.Collapsed;
            //游戏结束，隐藏
            bdrEndOfGame.Visibility = Visibility.Collapsed;

            //清除原来的蛇
            foreach (SnakePart snakeBodyPart in snakeParts)
            {
                if (snakeBodyPart.UiElement != null)
                {
                    GameArea.Children.Remove(snakeBodyPart.UiElement);
                }
            }
            snakeParts.Clear();

            //清除残留的食物
            if (snakeFood != null)
            {
                GameArea.Children.Remove(snakeFood);
            }

            //重设参数
            //当前分数置0
            currentScore = 0;
            //蛇的长度，置默认值
            snakeLength = SnakeStartLength;
            //蛇移动的方向，置右
            snakeDirection = SnakeDirection.Right;
            //为蛇添加一节身体，放在5,5位置
            snakeParts.Add(new SnakePart() { Position = new Point(SnakeSquareSize * 5, SnakeSquareSize * 5) });
            //设置计时器
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(SnakeStartSpeed);
            //画蛇
            DrawSnake();
            //画食物
            DrawSnakeFood();
            //更新游戏状态
            UpdateGameStatus();
            //启动定时器，游戏开始运行
            gameTickTimer.IsEnabled = true;
            //游戏开始运行
            isGameRunning = true;
        }

        /// <summary>
        /// 定时器中断，移动蛇
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GameTickTimer_Tick(object sender, EventArgs e)
        {
            if (isGameRunning == false) return;

            MoveSnake();
        }

       
        /// <summary>
        /// 取一个可以放食物的坐标
        /// </summary>
        /// <returns></returns>
        private Point GetNextFoodPosition()
        {
            int maxX = (int)(GameArea.ActualWidth / SnakeSquareSize);
            int maxY = (int)(GameArea.ActualHeight / SnakeSquareSize);
            int foodX = rnd.Next(0, maxX) * SnakeSquareSize;
            int foodY = rnd.Next(0, maxY) * SnakeSquareSize;

            //不要放在蛇身上
            foreach (SnakePart snakePart in snakeParts)
            {
                if ((snakePart.Position.X == foodX) && (snakePart.Position.Y == foodY))
                {
                    //在蛇身上,重新寻找位置
                    return GetNextFoodPosition();
                }
            }

            //找到一个空位，可以放食物
            return new Point(foodX, foodY);
        }

        /// <summary>
        /// 画棋盘
        /// </summary>
        private void DrawGameArea()
        {
            int nextX = 0;
            int nextY = 0;
            int rowCounter = 0;
            bool nextIsOdd = false;

            while (true)
            {
                //棋盘中的方格，黑白相间
                Rectangle rect = new Rectangle
                {
                    Width = SnakeSquareSize,
                    Height = SnakeSquareSize,
                    Fill = nextIsOdd ? Brushes.White : Brushes.LightGray
                };

                GameArea.Children.Add(rect);
                Canvas.SetTop(rect, nextY);
                Canvas.SetLeft(rect, nextX);

                //黑白相间
                nextIsOdd = !nextIsOdd;
                //先X方向排列
                nextX += SnakeSquareSize;
                //如果X方向排满，则Y方向移动一格
                if (nextX >= GameArea.ActualWidth)
                {
                    nextX = 0;
                    nextY += SnakeSquareSize;
                    rowCounter++;
                    nextIsOdd = (rowCounter % 2 != 0);
                }

                //如果Y方向已经排满，则棋盘已经排满
                if (nextY >= GameArea.ActualHeight)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 把蛇画出来
        /// </summary>
        private void DrawSnake()
        {
            foreach (SnakePart snakePart in snakeParts)
            {
                if (snakePart.UiElement == null)
                {
                    snakePart.UiElement = new Rectangle()
                    {
                        Width = SnakeSquareSize,
                        Height = SnakeSquareSize,
                        Fill = (snakePart.IsHead ? snakeHeadBrush : snakeBodyBrush)
                    };


                    GameArea.Children.Add(snakePart.UiElement);
                    Canvas.SetTop(snakePart.UiElement, snakePart.Position.Y);
                    Canvas.SetLeft(snakePart.UiElement, snakePart.Position.X);
                }
            }
        }
        /// <summary>
        /// 蛇向前爬行
        /// </summary>
        private void MoveSnake()
        {
            //删除蛇尾
            while (snakeParts.Count >= snakeLength)
            {
                GameArea.Children.Remove(snakeParts[0].UiElement);
                snakeParts.RemoveAt(0);
            }

            //给蛇身子涂上颜色
            foreach (SnakePart snakePart in snakeParts)
            {
                (snakePart.UiElement as Rectangle).Fill = snakeBodyBrush;
                snakePart.IsHead = false;
            }

            //判断蛇向哪个方向前进，准备添加新蛇头
            SnakePart snakeHead = snakeParts[snakeParts.Count - 1];
            double nextX = snakeHead.Position.X;
            double nextY = snakeHead.Position.Y;
            switch (snakeDirection)
            {
                case SnakeDirection.Left:
                    nextX -= SnakeSquareSize;
                    break;
                case SnakeDirection.Right:
                    nextX += SnakeSquareSize;
                    break;
                case SnakeDirection.Up:
                    nextY -= SnakeSquareSize;
                    break;
                case SnakeDirection.Down:
                    nextY += SnakeSquareSize;
                    break;
            }

            //添加新蛇头
            snakeParts.Add(new SnakePart()
            {
                Position = new Point(nextX, nextY),
                IsHead = true
            });

            //把蛇画出来 
            DrawSnake();

            //碰撞检测
            DoCollisionCheck();
        }

        /// <summary>
        /// 检测窗口按键
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            SnakeDirection originalSnakeDirection = snakeDirection;
            switch (e.Key)
            {
                case Key.Up:
                    if (snakeDirection != SnakeDirection.Down)
                        snakeDirection = SnakeDirection.Up;
                    break;
                case Key.Down:
                    if (snakeDirection != SnakeDirection.Up)
                        snakeDirection = SnakeDirection.Down;
                    break;
                case Key.Left:
                    if (snakeDirection != SnakeDirection.Right)
                        snakeDirection = SnakeDirection.Left;
                    break;
                case Key.Right:
                    if (snakeDirection != SnakeDirection.Left)
                        snakeDirection = SnakeDirection.Right;
                    break;
                case Key.Space:
                    StartNewGame();
                    break;
                default:
                    return;
            }
            if (snakeDirection != originalSnakeDirection)
                MoveSnake();
        }

        /// <summary>
        /// 碰撞检测
        /// </summary>
        private void DoCollisionCheck()
        {
            //取蛇头
            SnakePart snakeHead = snakeParts[snakeParts.Count - 1];

            //如果蛇头与食物重合，则吃食物
            if ((snakeHead.Position.X == Canvas.GetLeft(snakeFood)) && (snakeHead.Position.Y == Canvas.GetTop(snakeFood)))
            {
                EatSnakeFood();
                return;
            }

            //如果蛇头出界，游戏结束
            if (
                (snakeHead.Position.Y < 0) || 
                (snakeHead.Position.Y >= GameArea.ActualHeight) ||
                (snakeHead.Position.X < 0) || 
                (snakeHead.Position.X >= GameArea.ActualWidth)
                )
            {
                EndGame();
            }

            //如果蛇头与蛇身子重合，则游戏结束
            foreach (SnakePart snakeBodyPart in snakeParts.Take(snakeParts.Count - 1))
            {
                if (
                    (snakeHead.Position.X == snakeBodyPart.Position.X) &&
                    (snakeHead.Position.Y == snakeBodyPart.Position.Y)
                    )
                {
                    EndGame();
                }
            }
        }

        /// <summary>
        /// 蛇吃掉了一个食物
        /// </summary>
        private void EatSnakeFood()
        {
            //语音
            speechSynthesizer.SpeakAsync("yeah");
            //蛇的长度++
            snakeLength++;
            //当前分数++
            currentScore++;
            //调整定时器
            int timerInterval = Math.Max(SnakeSpeedThreshold, (int)gameTickTimer.Interval.TotalMilliseconds - (currentScore * 2));
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(timerInterval);
            //删除食物
            GameArea.Children.Remove(snakeFood);
            //创建新食物
            DrawSnakeFood();
            //更新游戏状态
            UpdateGameStatus();
        }

        /// <summary>
        /// 更新游戏状态
        /// </summary>
        private void UpdateGameStatus()
        {
            //this.Title = "SnakeWPF - Score: " + currentScore + " - Game speed: " + gameTickTimer.Interval.TotalMilliseconds;
            this.tbStatusScore.Text = currentScore.ToString();
            this.tbStatusSpeed.Text = gameTickTimer.Interval.TotalMilliseconds.ToString();
        }

        /// <summary>
        /// 游戏结束
        /// </summary>
        private void EndGame()
        {
            //gameTickTimer.IsEnabled = false;
            //MessageBox.Show("Oooops, you died!\n\nTo start a new game, just press the Space bar...", "SnakeWPF");

            //判断是否为新高分
            bool isNewHighscore = false;
            if (currentScore > 0)
            {
                //高分榜中的最低分
                int lowestHighscore = this.HighscoreList.Count > 0 ? this.HighscoreList.Min(x => x.Score) : 0;

                //如果分数大于高分榜中的最低分，或者高分榜中的记录数量还不超过最大值
                //显示添加新高分的界面
                if ((currentScore > lowestHighscore) || (this.HighscoreList.Count < MaxHighscoreListEntryCount))
                {
                    bdrNewHighscore.Visibility = Visibility.Visible;
                    txtPlayerName.Focus();
                    isNewHighscore = true;
                }
            }

            //如果不是新高分，则显示成绩
            if (!isNewHighscore)
            {
                tbFinalScore.Text = currentScore.ToString();
                bdrEndOfGame.Visibility = Visibility.Visible;
            }

            //停用定时器
            gameTickTimer.IsEnabled = false;

            //语音报分数
            SpeakEndOfGameInfo(isNewHighscore);
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 显示高分榜
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnShowHighscoreList_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                var father = VisualTreeHelper.GetParent(button);
                var grandpa =VisualTreeHelper.GetParent(father) as Border;
                grandpa.Visibility = Visibility.Collapsed;
            }

            bdrNewHighscore.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 添加到高分列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnAddToHighscoreList_Click(object sender, RoutedEventArgs e)
        {
            //确定插入高分榜中的位置
            int newIndex = 0;
            if ((this.HighscoreList.Count > 0) && (currentScore < this.HighscoreList.Max(x => x.Score)))
            {
                SnakeHighscore justAbove = this.HighscoreList.OrderByDescending(x => x.Score).First(x => x.Score >= currentScore);
                if (justAbove != null)
                    newIndex = this.HighscoreList.IndexOf(justAbove) + 1;
            }

            //向高分榜，插入一条新记录
            this.HighscoreList.Insert(newIndex, new SnakeHighscore()
            {
                PlayerName = txtPlayerName.Text,
                Score = currentScore
            });

            //让高分榜不超过最大数量
            while (this.HighscoreList.Count > MaxHighscoreListEntryCount)
            {
                this.HighscoreList.RemoveAt(MaxHighscoreListEntryCount);
            }

            //保存高分榜记录到文件
            SaveHighscoreList();

            //显示高分榜
            bdrNewHighscore.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 加载高分列表
        /// </summary>
        private void LoadHighscoreList()
        {
            if (File.Exists(snake_highscorelist_file))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<SnakeHighscore>));
                using (Stream reader = new FileStream(snake_highscorelist_file, FileMode.Open))
                {
                    //反序列化
                    List<SnakeHighscore> tempList = (List<SnakeHighscore>)serializer.Deserialize(reader);
                    //清除高分列表
                    this.HighscoreList.Clear();
                    //根据分数排序，添加到高分列表
                    foreach (var item in tempList.OrderByDescending(x => x.Score))
                    {
                        this.HighscoreList.Add(item);
                    }
                }
            }
        }

        /// <summary>
        /// 保存高分列表
        /// </summary>
        private void SaveHighscoreList()
        {
            //序列化,保存到文件
            XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<SnakeHighscore>));
            using (Stream writer = new FileStream(snake_highscorelist_file, FileMode.Create))
            {
                serializer.Serialize(writer, this.HighscoreList);
            }
        }

        /// <summary>
        /// 语音报分数
        /// </summary>
        /// <param name="isNewHighscore"></param>
        private void SpeakEndOfGameInfo(bool isNewHighscore)
        {
            PromptBuilder promptBuilder = new PromptBuilder();

            promptBuilder.StartStyle(new PromptStyle()
            {
                Emphasis = PromptEmphasis.Reduced,
                Rate = PromptRate.Slow,
                Volume = PromptVolume.ExtraLoud
            });
            promptBuilder.AppendText("Game Over!");
            //promptBuilder.AppendBreak(TimeSpan.FromMilliseconds(200));
            promptBuilder.AppendText("你的成绩是：");
            promptBuilder.AppendTextWithHint(currentScore.ToString(), SayAs.NumberCardinal);
            promptBuilder.AppendText("分！");
            promptBuilder.EndStyle();

            //新高分
            if (isNewHighscore)
            {
                //promptBuilder.AppendBreak(TimeSpan.FromMilliseconds(500));
                promptBuilder.StartStyle(new PromptStyle()
                {
                    Emphasis = PromptEmphasis.Moderate,
                    Rate = PromptRate.ExtraFast,
                    Volume = PromptVolume.Medium
                });
                promptBuilder.AppendText("恭喜你！获得新高分！");
                //promptBuilder.AppendBreak(TimeSpan.FromMilliseconds(200));
                promptBuilder.AppendTextWithHint(currentScore.ToString(), SayAs.NumberCardinal);
                promptBuilder.AppendText("分！");
                promptBuilder.EndStyle();
            }
            speechSynthesizer.SpeakAsync(promptBuilder);
        }

        /// <summary>
        /// 按照鼠标左键，拖动窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        //游戏暂停，游戏继续
        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                if(button.Content.ToString()=="II")
                {
                    isGameRunning = false;
                    button.Content = "▶";
                }
                else
                {
                    isGameRunning = true;
                    button.Content = "II";
                }
            }
        }
    }
}
