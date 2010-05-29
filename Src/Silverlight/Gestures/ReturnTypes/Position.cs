﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;


namespace TouchToolkit.GestureProcessor.ReturnTypes
{
    public class Position : IReturnType
    {
        public double X { get; set; }
        public double Y { get; set; }

        public override string ToString()
        {
            return string.Format("X:{0},Y:{1}", X, Y);
        }
    }
}
