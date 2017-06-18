using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Tetris
{
    internal class Vasilyeva_Veronika
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("You should specify filename in cmd arguments");
                return;
            }

            using (var reader = new StreamReader(args[0]))
            {
                var json = reader.ReadToEnd();

                JsonConvert.DeserializeObject<GameSettings>(json);
                var game = Game.Start();

                while (game.HasNextCommand(game))
                {
                    game = game.DoNextStep();
                }
            }
        }
    }

    internal class GameSettings
    {
        public static int Width { get; private set; }

        public static int Height { get; private set; }

        public static ImmutableArray<Figure> Pieces { get; private set; }

        public static string Commands { get; private set; }

        public GameSettings(int width, int height, IEnumerable<Figure> pieces, string commands)
        {
            Width = width;
            Height = height;
            Pieces = pieces.ToImmutableArray();
            Commands = commands;
        }
    }

    internal class Game
    {
        private readonly Figure _currentFigure;
        private readonly Point _currentCoordinate;
        private readonly Field _gameField;

        private readonly int _score;
        private readonly int _commandNumber;
        private readonly int _currentFigureNumber;

        private Game(Field field, int score, int commandNumber, int figureNumber)
        {
            _gameField = field;
            _currentFigureNumber = figureNumber % GameSettings.Pieces.Length;
            _currentFigure = GameSettings.Pieces[_currentFigureNumber];
            _currentCoordinate = GetStartPosition(_currentFigure);
            _score = score;
            _commandNumber = commandNumber;

            if (CantPlaceFigureAtPosition(_currentFigure, _currentCoordinate))
            {
                _score = score - 10;
                _gameField = new Field(ImmutableHashSet<Point>.Empty, new int[GameSettings.Height].ToImmutableList());
            }
        }

        private Game(Figure figure, Field field, Point coordinate, int score, int commandNumber, int figureNumber)
        {
            _currentFigure = figure;
            _currentCoordinate = coordinate;
            _gameField = field;
            _score = score;
            _commandNumber = commandNumber;
            _currentFigureNumber = figureNumber % GameSettings.Pieces.Length;
        }

        public static Game Start()
        {
            return new Game(new Field(ImmutableHashSet<Point>.Empty, new int[GameSettings.Height].ToImmutableList()), 0, 0, 0);
        }

        private bool CommandIsRotate(char command)
        {
            return (command == 'Q' || command == 'E');
        }
        public Game DoNextStep()
        {
            var newCommand = GameSettings.Commands.ElementAt(_commandNumber);

            if (newCommand == 'P')
            {
                CommandPrint();
                return new Game(_currentFigure, _gameField, _currentCoordinate, _score, _commandNumber + 1,
                    _currentFigureNumber);
            }

            var newFigure = CommandIsRotate(newCommand)
                ? CommandRotate(newCommand)
                : _currentFigure;

            var newCoordinate = CommandIsRotate(newCommand)
                ? _currentCoordinate
                : CommandShift(newCommand);

            if (CantPlaceFigureAtPosition(newFigure, newCoordinate))
            {
                int newScore;
                var newField = FixFigureOnField(_currentFigure, _currentCoordinate, out newScore);

                var newGame = new Game(newField, _score + newScore, _commandNumber + 1, _currentFigureNumber + 1);
                Console.WriteLine(_commandNumber + " " + newGame._score);

                return newGame;
            }
            return new Game(newFigure, _gameField, newCoordinate, _score, _commandNumber + 1, _currentFigureNumber);
        }

        private bool CantPlaceFigureAtPosition(Figure figure, Point coordinate)
        {
            var coordinateFigure = figure.Cells.Select(i => i + coordinate).ToImmutableArray();

            var minX = coordinateFigure.Min(i => i.X);
            var maxX = coordinateFigure.Max(i => i.X);
            var minY = coordinateFigure.Min(i => i.Y);
            var maxY = coordinateFigure.Max(i => i.Y);

            if (minX < 0 || minY < 0 || maxX > GameSettings.Width - 1 || maxY > GameSettings.Height - 1) return true;

            return coordinateFigure.Any(point => _gameField.FilledCells.Contains(point));
        }

        private Field FixFigureOnField(Figure figure, Point coordinate, out int score)
        {
            var coordinateFigure = figure.Cells.Select(i => i + coordinate).ToImmutableHashSet();
            var newFilledCells = _gameField.FilledCells.Union(coordinateFigure);

            var groupedByRows = coordinateFigure.GroupBy(p => p.Y).ToImmutableDictionary(g => g.Key, g => g.Count());

            var newAmountFilledCells = groupedByRows.Aggregate(_gameField.AmountFilledCells,
                (array, row) => array.SetItem(row.Key, array[row.Key] + row.Value));

            var cells = newAmountFilledCells;
            var fullRows = groupedByRows.Keys.Where(row => cells[row] == GameSettings.Width).ToImmutableArray().Sort();
            score = fullRows.Length;

            foreach (var y in fullRows)
            {
                var yCopy = y;
                newFilledCells = newFilledCells
                    .Where(i => i.Y != yCopy)
                    .Select(j => j.Y < yCopy ? new Point(j.X, j.Y + 1) : j)
                    .ToImmutableHashSet();

                newAmountFilledCells = Enumerable.Range(0, y).Aggregate(newAmountFilledCells,
                    (current, index) => current.SetItem(y - index, current[y - index - 1]));
                newAmountFilledCells = newAmountFilledCells.SetItem(0, 0);
            }
            return new Field(newFilledCells, newAmountFilledCells);
        }

        public bool HasNextCommand(Game game)
        {
            return game._commandNumber < GameSettings.Commands.Length;
        }

        private static Point GetStartPosition(Figure figure)
        {
            var leftX = figure.Cells.Min(i => i.X);
            var rightX = figure.Cells.Max(i => i.X);
            var startX = (GameSettings.Width - (1 + rightX - leftX)) / 2;

            return new Point(startX - leftX, -figure.Cells.Min(i => i.Y));
        }

        private static readonly ImmutableDictionary<char, Point> Directions = new Dictionary<char, Point>
        {
            {'A', new Point(-1, 0)},
            {'D', new Point(1, 0)},
            {'S', new Point(0, 1)}
        }.ToImmutableDictionary();

        private Point CommandShift(char command)
        {
            return _currentCoordinate + Directions[command];
        }

        private Figure CommandRotate(char command)
        {
            return
                new Figure(
                    _currentFigure.Cells.Select(i => command == 'Q' ? new Point(i.Y, -i.X) : new Point(-i.Y, i.X))
                        .ToImmutableArray());
        }

        private void CommandPrint()
        {
            var coordinateFigure = _currentFigure.Cells.Select(i => i + _currentCoordinate).ToImmutableHashSet();

            var outString = Enumerable.Range(0, GameSettings.Height)
                .Select(i => Enumerable.Range(0, GameSettings.Width)
                    .Select(j => _gameField.FilledCells.Contains(new Point(j, i))
                        ? "#"
                        : coordinateFigure.Contains(new Point(j, i)) ? "*" : "."
                    ).Aggregate((x, y) => x + y) + "\n")
                .Aggregate((x, y) => x + y);

            Console.WriteLine(outString.Substring(0, outString.Length - 1));
        }
    }

    internal class Point
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static Point operator +(Point p1, Point c2)
        {
            return new Point(p1.X + c2.X, p1.Y + c2.Y);
        }

        public override bool Equals(object obj)
        {
            var point = obj as Point;
            return point != null && X == point.X && Y == point.Y;
        }

        public override int GetHashCode()
        {
            return X ^ Y;
        }
    }

    internal class Figure
    {
        public IImmutableList<Point> Cells { get; private set; }

        public Figure(IEnumerable<Point> cells)
        {
            Cells = cells.ToImmutableList();
        }
    }

    internal class Field
    {
        public ImmutableHashSet<Point> FilledCells { get; private set; }
        public IImmutableList<int> AmountFilledCells { get; private set; }

        public Field(ImmutableHashSet<Point> arr, IImmutableList<int> arr2)
        {
            FilledCells = arr;
            AmountFilledCells = arr2;
        }
    }
}
