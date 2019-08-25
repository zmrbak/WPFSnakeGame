using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
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
        //方格大小
        const int SnakeSquareSize = 20;
        //蛇身颜色
        SolidColorBrush snakeBodyBrush = Brushes.Green;
        //蛇头颜色
        SolidColorBrush snakeHeadBrush = Brushes.Blue;
        //蛇身
        List<SnakePart> snakeParts = new List<SnakePart>();
        //爬行方向
        SnakeDirection snakeDirection = SnakeDirection.Right;
        //蛇的长度
        int snakeLength = 0;
        //定时器
        DispatcherTimer gameTickTimer = new DispatcherTimer();
        //蛇初始长度
        const int SnakeStartLength = 3;
        //蛇初始速度
        const int SnakeStartSpeed = 500;
        //蛇的最大速速
        const int SnakeSpeedThreshold = 100;
        //随机数
        Random random = new Random();
        //最大
        int maxX = 0;
        int maxY = 0;
        //蛇的食物
        UIElement snakeFood = null;
        //食物颜色
        SolidColorBrush foodBrush = Brushes.Red;
        //分数
        int currentScore = 0;
        //游戏是否在运行
        Boolean isGameRuning = true;
        //高分列表
        List<SnakeHighscore> HighscoreList = new List<SnakeHighscore>();
        //存盘文件
        String snake_highscorelist_xml = "snake_highscorelist.xml";
        //高分列表数量
        const int MaxHighscoreListEntryCount = 5;
        //语音合成
        private SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer();


        public MainWindow()
        {
            InitializeComponent();
            gameTickTimer.Tick += GameTickTimer_Tick;
        }

        private void GameTickTimer_Tick(object sender, EventArgs e)
        {
            if (isGameRuning == false) return;

            MoveSnake();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DrawGameArea();

            maxX = (int)(GameArea.ActualWidth / SnakeSquareSize);
            maxY = (int)(GameArea.ActualHeight / SnakeSquareSize);

            bdrEndOfGame.Visibility = Visibility.Collapsed;
            bdrWelcomeMessage.Visibility = Visibility.Visible;
        }

        private void DrawGameArea()
        {
            int nextX = 0;
            int nextY = 0;

            Boolean netxtIsOdd = false;

            while (true)
            {
                Rectangle rectangle = new Rectangle
                {
                    Width = SnakeSquareSize,
                    Height = SnakeSquareSize,
                    Fill = netxtIsOdd ? Brushes.White : Brushes.LightGray
                };

                GameArea.Children.Add(rectangle);
                Canvas.SetTop(rectangle, nextY);
                Canvas.SetLeft(rectangle, nextX);

                netxtIsOdd = !netxtIsOdd;
                nextX += SnakeSquareSize;

                if(nextX>= GameArea.ActualWidth)
                {
                    nextX = 0;
                    nextY += SnakeSquareSize;
                    netxtIsOdd = !netxtIsOdd;
                }

                if(nextY>=GameArea.ActualHeight)
                {
                    break;
                }
            }
        }

        private void DrawSnake()
        {
            foreach (var snakePart in snakeParts)
            {
                if (snakePart.UiElement == null)
                {
                    snakePart.UiElement = new Rectangle
                    {
                        Width = SnakeSquareSize,
                        Height = SnakeSquareSize,
                        Fill = snakePart.IsHead ? snakeHeadBrush : snakeBodyBrush
                    };
                    GameArea.Children.Add(snakePart.UiElement);
                    Canvas.SetTop(snakePart.UiElement, snakePart.Position.Y);
                    Canvas.SetLeft(snakePart.UiElement,snakePart.Position.X);
                }
            }
        }

        private void MoveSnake()
        {
            //a)	删掉尾巴
            while (snakeParts.Count>=snakeLength)
            {
                GameArea.Children.Remove(snakeParts[0].UiElement);
                snakeParts.RemoveAt(0);
            }

            //b)	蛇头换上蛇身子的颜色
            foreach (var snakePart in snakeParts)
            {
                (snakePart.UiElement as Rectangle).Fill = snakeBodyBrush;
                snakePart.IsHead = false;
            }

            //c)	找个地方，添个新蛇头
            SnakePart snakeHead = snakeParts[snakeParts.Count -1];
            double nextX = snakeHead.Position.X;
            double nextY = snakeHead.Position.Y;

            switch(snakeDirection)
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
                default:
                    break;
            }
            //，添个新蛇头
            snakeParts.Add(new SnakePart
            {
                Position = new Point(nextX, nextY),
                IsHead=true
            }
            ) ;

            //d)	重新画出来
            DrawSnake();

            //判断是否撞墙
            DoCollisionCheck();
        }

        private void DoCollisionCheck()
        {
            SnakePart snakeHead = snakeParts[snakeParts.Count - 1];
            //蛇吃食物
            if((snakeHead.Position.X == Canvas.GetLeft(snakeFood))&&
                (snakeHead.Position.Y == Canvas.GetTop(snakeFood))
                )
            {
                EatSnakeFood();
                return;
            }

            //蛇有没有越界
            if((snakeHead.Position.Y < 0)||
                (snakeHead.Position.X < 0) ||
                (snakeHead.Position.Y >= GameArea.ActualHeight)||
                (snakeHead.Position.X >= GameArea.ActualWidth)
                )
            {
                EndGame();
                return;
            }

            //蛇有没有撞到自己的身体
            foreach (var snakeBodyPart in snakeParts.Take(snakeParts.Count - 1))
            {
                if( (snakeHead.Position.X == snakeBodyPart.Position.X) &&
                    (snakeHead.Position.Y == snakeBodyPart.Position.Y)
                    )
                {
                    EndGame();
                    return;
                }
            }
        }

        private void EndGame()
        {
            MediaPlayer mediaPlayer = new MediaPlayer();
            mediaPlayer.Open(new Uri("cry.wav",UriKind.RelativeOrAbsolute));
            mediaPlayer.Play();

            //判断是否为新高分
            bool isNewHighscore = false;

            if (currentScore > 0)
            {
                //高分榜中的最低分
                int lowestHighscore = HighscoreList.Count > 0 ? HighscoreList.Min(x => x.Score) : 0;

                //如果分数大于高分榜中的最低分，或者高分榜中的记录数量还不超过最大值
                //显示添加新高分的界面
                if ((currentScore > lowestHighscore)||(HighscoreList.Count < MaxHighscoreListEntryCount))
                {
                    bdrNewHighscore.Visibility = Visibility.Visible;
                    txtPlayerName.Focus();
                    isNewHighscore = true;

                    //让游戏停下来
                    isGameRuning = false;

                    //恭喜你，进入了高分榜
                    PromptBuilder promptBuilder = new PromptBuilder();
                    promptBuilder.AppendText("恭喜你，进入了高分榜");
                    promptBuilder.AppendTextWithHint(currentScore.ToString(), SayAs.NumberCardinal);
                    promptBuilder.AppendText("分");
                    speechSynthesizer.SpeakAsync(promptBuilder);
                }
            }
            //如果不是新高分，则显示成绩
            if (isNewHighscore==false)
            {
                tbFinalScore.Text = currentScore.ToString();
                bdrEndOfGame.Visibility = Visibility.Visible;

                PromptBuilder promptBuilder = new PromptBuilder();
                promptBuilder.AppendText("你的得分：");
                promptBuilder.AppendTextWithHint(currentScore.ToString(),SayAs.NumberCardinal);
                promptBuilder.AppendText("分");
                speechSynthesizer.SpeakAsync(promptBuilder);
            }

            gameTickTimer.IsEnabled = false;
        }

        private void EatSnakeFood()
        {
            snakeLength++;
            currentScore++;
            //增加游戏难度
            int timerInterval = Math.Max(SnakeSpeedThreshold, (int)(gameTickTimer.Interval.TotalMilliseconds) - (currentScore * 2));
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(timerInterval);

            GameArea.Children.Remove(snakeFood);
            DrawSnakeFood();

            UpdateGameStatus();

            SystemSounds.Beep.Play();
        }

        private void UpdateGameStatus()
        {
            tbStatusScore.Text = currentScore.ToString();
            tbStatusSpeed.Text = gameTickTimer.Interval.TotalMilliseconds.ToString();
        }

        private void StartNewGame()
        {
            //清除残存的蛇的身体
            foreach (var snakeBodyPart in snakeParts)
            {
                if(snakeBodyPart.UiElement!=null)
                {
                    GameArea.Children.Remove(snakeBodyPart.UiElement);
                }
            }
            snakeParts.Clear();

            //清除残存的食物
            if(snakeFood !=null)
            {
                GameArea.Children.Remove(snakeFood);
            }
            snakeFood = null;

            currentScore = 0;
            snakeLength = SnakeStartLength;
            snakeDirection = SnakeDirection.Right;
            snakeParts.Add(new SnakePart { Position = new Point(SnakeSquareSize * 5, SnakeSquareSize * 5) });
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(SnakeStartSpeed);

            DrawSnake();

            DrawSnakeFood();

            UpdateGameStatus();

            gameTickTimer.IsEnabled = true;
            gameTickTimer.Start();

            btPause.Visibility = Visibility.Visible;

            bdrWelcomeMessage.Visibility = Visibility.Collapsed;
            bdrEndOfGame.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Collapsed;
        }

        private Point GetNextFoodPosition()
        {
            int foodX = random.Next(0, maxX) * SnakeSquareSize;
            int foodY = random.Next(0, maxY) * SnakeSquareSize;

            foreach (var snakePart in snakeParts)
            {
                if ((snakePart.Position.X == foodX) && (snakePart.Position.Y == foodY))
                {
                    return GetNextFoodPosition();
                }
            }

            return new Point(foodX, foodY);
        }

        private void DrawSnakeFood()
        {
            Point foodPosition = GetNextFoodPosition();
            snakeFood = new Ellipse 
            {
                Width= SnakeSquareSize,
                Height= SnakeSquareSize,
                Fill= foodBrush
            };
            GameArea.Children.Add(snakeFood);
            Canvas.SetLeft(snakeFood,foodPosition.X);
            Canvas.SetTop(snakeFood,foodPosition.Y);
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (isGameRuning == false) return;

            SnakeDirection originalSnakeDirection = snakeDirection;
            switch(e.Key)
            {
                case Key.Up:
                    if(snakeDirection!= SnakeDirection.Down)
                    {
                        snakeDirection = SnakeDirection.Up;
                    }
                    break;
                case Key.Down:
                    if (snakeDirection != SnakeDirection.Up)
                    {
                        snakeDirection = SnakeDirection.Down;
                    }
                    break;
                case Key.Left:
                    if (snakeDirection != SnakeDirection.Right)
                    {
                        snakeDirection = SnakeDirection.Left;
                    }
                    break;
                case Key.Right:
                    if (snakeDirection != SnakeDirection.Left)
                    {
                        snakeDirection = SnakeDirection.Right;
                    }
                    break;
                case Key.Space:
                    StartNewGame();
                    break;
                default:
                    return;
            }

            if (snakeDirection != originalSnakeDirection)
            {
                MoveSnake();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void BtPause_Click(object sender, RoutedEventArgs e)
        {
            //游戏暂停
            if (isGameRuning==false)
            {
                isGameRuning = true;
                btPause.Content = "II";
            }
            else
            {
                isGameRuning = false;
                btPause.Content = "▶";
            }
        }

        private void BtnShowHighscoreList_Click(object sender, RoutedEventArgs e)
        {
            bdrHighscoreList.Visibility = Visibility.Visible;
            bdrWelcomeMessage.Visibility = Visibility.Collapsed;
        }
        private void SaveHighscoreList()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<SnakeHighscore>));
            using (Stream writer = new FileStream(snake_highscorelist_xml,FileMode.Create) )
            {
                serializer.Serialize(writer,  new List<SnakeHighscore>(HighscoreList.OrderByDescending(x=>x.Score).Take(MaxHighscoreListEntryCount)));
            }
        }

        private void LoadHighscoreList()
        {
            HighscoreList = new List<SnakeHighscore>();
            if (File.Exists(snake_highscorelist_xml))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<SnakeHighscore>));
                using (Stream writer = new FileStream(snake_highscorelist_xml, FileMode.Open))
                {
                    HighscoreList = (List<SnakeHighscore>)serializer.Deserialize(writer);
                }
            }
            bdrHighscoreListItems.ItemsSource = HighscoreList;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadHighscoreList();
            bdrNewHighscore.Visibility = Visibility.Collapsed;
        }

        private void BtnAddToHighscoreList_Click(object sender, RoutedEventArgs e)
        {
            HighscoreList.Add(new SnakeHighscore {  PlayerName= txtPlayerName.Text, Score=currentScore});
            HighscoreList = new List<SnakeHighscore>(HighscoreList.OrderByDescending(x => x.Score).Take(MaxHighscoreListEntryCount));
            bdrHighscoreListItems.ItemsSource = HighscoreList;

            txtPlayerName.Text = "";

            SaveHighscoreList();

            bdrNewHighscore.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Visible;

            isGameRuning = true;
        }
    }
}
