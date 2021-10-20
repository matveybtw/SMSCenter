using System;
using System.Collections.Generic;

namespace MenuNamespace
{
    public struct COORD
    {
        public int x;
        public int y;
    }
    public class Menu
    {
        public List<string> list;
        public COORD c;
        private int pos;
        public Menu(List<string> menu)
        {
            c.x = 0;
            c.y = 0;
            list = menu;
            pos = 0;
        }
        public Menu(List<string> menu, COORD coords)
        {
            c.x = coords.x;
            c.y = coords.y;
            list = menu;
            pos = 0;
        }
        public void ShowMenu()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            for (int i = 1; i < list.Count; i++)
            {
                Console.SetCursorPosition(c.x, c.y + i);
                Console.Write(list[i]);
            }
            Console.SetCursorPosition(c.x, c.y);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(list[0]);
            Console.ForegroundColor = ConsoleColor.White;
        }
        public int StartMenu()
        {
            pos = 0;
            ShowMenu();
            bool end = false;
            do
            {
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.UpArrow:
                            if (pos > 0)
                            {
                                Up();
                            }
                            break;
                        case ConsoleKey.DownArrow:
                            if (pos < (list.Count - 1))
                            {
                                Down();
                            }
                            break;
                        case ConsoleKey.Enter:
                            Console.Clear();
                            Console.ForegroundColor = ConsoleColor.White;
                            return pos;
                        case ConsoleKey.Escape:
                            end = true;
                            break;
                        default:
                            break;
                    }

                }
            } while (!end);
            return -1;
        }
        private void Up()
        {
            Console.SetCursorPosition(c.x, c.y + pos);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(list[pos]);
            pos--;
            Console.SetCursorPosition(c.x, c.y + pos);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(list[pos]);
        }
        private void Down()
        {
            Console.SetCursorPosition(c.x, c.y + pos);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(list[pos]);
            pos++;
            Console.SetCursorPosition(c.x, c.y + pos);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(list[pos]);
        }
    }
}
