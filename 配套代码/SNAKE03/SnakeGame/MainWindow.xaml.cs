﻿using System;
using System.Collections.Generic;
using System.Linq;
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


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DrawGameArea();
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
    }
}
