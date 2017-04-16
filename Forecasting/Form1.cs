using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Windows.Forms.DataVisualization.Charting;

namespace Forecasting
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Read SwordsDemand.csv containing 36 rows
            var dataSetSize = File.ReadAllLines("SwordsDemand.csv").Count();
            var dataSet = File.ReadAllLines("SwordsDemand.csv").Select(
                    line => double.Parse(line.Split(',').ElementAt(1)))
                .ToList();

            for (var i = 0; i < dataSet.Count; i++)
            {
                Chart1.Series["Data"].Points.AddXY(i + 1, dataSet[i]);
            }

            var dataSetSES = CalculateSES(dataSet, 12);
            for (var i = 0; i < dataSetSES.Item2.Count; i++)
            {
                Chart1.Series["SES"].Points.AddXY(i + 1, dataSetSES.Item2[i]);
            }

            var dataSetDES = CalculateDES(dataSet, 12);
            for (var i = 0; i < dataSetDES.Item2.Count; i++)
            {
                Chart1.Series["DES"].Points.AddXY(i + 2, dataSetDES.Item2[i]);
            }

            textBox1.AppendText("Best Alpha for SES: " + dataSetSES.Item1.Item1 + Environment.NewLine);
            textBox1.AppendText("Best Error for SES: " + dataSetSES.Item1.Item2 + Environment.NewLine + Environment.NewLine);
            textBox1.AppendText("Best Alpha for DES: " + dataSetDES.Item1.Item1 + Environment.NewLine);
            textBox1.AppendText("Best Beta for DES: " + dataSetDES.Item1.Item2 + Environment.NewLine);
            textBox1.AppendText("Best Alpha for DES: " + dataSetDES.Item1.Item3 + Environment.NewLine);
        }

        private Tuple<Tuple<double, double>, List<double>> CalculateSES(List<double> dataSet, int forecastAmount)
        {
            //var alphaSESFinal = 0.0;
            var alphaErrorSES = new List<Tuple<double, double>>();

            for (var i = 0.0; i < 1.0; i += 0.1) 
            {
                var alphaSES = i;
                var SSE = 0.0;
                var dataSetSES = new List<double> { dataSet[0] };
                
                for (var j = 1; j < dataSet.Count; j++)
                {
                    var smoothedPointSES = (alphaSES * dataSet[j - 1]) + (1 - alphaSES) * (dataSetSES[j - 1]);
                    dataSetSES.Add(smoothedPointSES);
                    SSE += Math.Pow((smoothedPointSES - dataSet[j]), 2);
                }
                SSE = Math.Sqrt(SSE / (dataSet.Count - 1));
                alphaErrorSES.Add(new Tuple<double, double>(i, SSE));
            }

            var alphaErrorSESFinal = alphaErrorSES.Aggregate((l, r) => (l.Item2 < r.Item2) ? l : r);
            var alphaSESFinal = alphaErrorSESFinal.Item1;

            var dataSetSESFinal = new List<double> { dataSet[0] };
            for (var i = 1; i < dataSet.Count + forecastAmount; i++)
            {
                var smoothedPointSES = 0.0;
                if (i >= dataSet.Count) 
                    smoothedPointSES = dataSetSESFinal[dataSet.Count - 1];
                else
                    smoothedPointSES = (alphaSESFinal * dataSet[i]) + (1 - alphaSESFinal) * (dataSetSESFinal[i - 1]);
                dataSetSESFinal.Add(smoothedPointSES);
            }
            var resultsSES = new Tuple<Tuple<double, double>, List<double>>(alphaErrorSESFinal, dataSetSESFinal);
            return resultsSES;
        }

        private Tuple<Tuple<double, double, double>, List<double>> CalculateDES(List<double> dataSet, int forecastAmount)
        {
            var alphaBetaErrorDES = new List<Tuple<double, double, double>>();

            for (var i = 0.0; i < 1.0; i += 0.1)
            {
                var alphaDES = i;

                for (var j = 0.0; j < 1.0; j += 0.1) 
                {
                    var betaDES = j;
                    var dataSetDES = new List<double> { dataSet[0], dataSet[1] };
                    var dataSetTrendDES = new List<double> { 0, dataSet[1] - dataSet[0] };
                    var dataSetForecastDES = new List<double>();
                    var SSE = 0.0;
                    for (var k = 2; k < dataSet.Count; k++)
                    {
                        var smoothedPointDES = (alphaDES * dataSet[k]) + (1 - alphaDES) * (dataSetDES[k - 1] + dataSetTrendDES[k - 1]);
                        dataSetDES.Add(smoothedPointDES);
                        var trendPointDES = (betaDES * (dataSetDES[k] - dataSetDES[k - 1])) + (1 - betaDES) * dataSetTrendDES[k - 1];
                        dataSetTrendDES.Add(trendPointDES);
                        var forecastPointDES = dataSetDES[k-1] + dataSetTrendDES[k-1];
                        dataSetForecastDES.Add(forecastPointDES);
                        SSE += Math.Pow((forecastPointDES - dataSet[k]), 2);
                    }
                    SSE = Math.Sqrt(SSE / (dataSet.Count - 2));
                    alphaBetaErrorDES.Add(new Tuple<double, double, double>(i, j, SSE));
                }
            }
            foreach (var tuple in alphaBetaErrorDES) {
                Console.WriteLine(tuple);
            }
            var alphaErrorDESFinal = alphaBetaErrorDES.Aggregate((l, r) => (l.Item3 < r.Item3) ? l : r);
            var alphaDESFinal = alphaErrorDESFinal.Item1;
            var betaDESFinal = alphaErrorDESFinal.Item2;

            var dataSetDESFinal = new List<double> { dataSet[0], dataSet[1] };
            var dataSetTrendDESFinal = new List<double> { 0, dataSet[1] - dataSet[0] };
            var dataSetForecastDESFinal = new List<double>();

            for (var i = 2; i < dataSet.Count + forecastAmount; i++)
            {
                var smoothedPointDES = 0.0;
                var trendPointDES = 0.0;
                var forecastPointDES = 0.0;

                if (i >= dataSet.Count)
                {
                    forecastPointDES = dataSetDESFinal[dataSet.Count - 1] + (dataSetTrendDESFinal[dataSet.Count - 1] * (i - dataSet.Count));
                    dataSetForecastDESFinal.Add(forecastPointDES);
                }
                else 
                {
                    smoothedPointDES = (alphaDESFinal * dataSet[i]) + (1 - alphaDESFinal) * (dataSetDESFinal[i - 1] + dataSetTrendDESFinal[i - 1]);
                    dataSetDESFinal.Add(smoothedPointDES);
                    trendPointDES = (betaDESFinal * (dataSetDESFinal[i] - dataSetDESFinal[i - 1])) + (1 - betaDESFinal) * dataSetTrendDESFinal[i - 1];
                    dataSetTrendDESFinal.Add(trendPointDES);
                    forecastPointDES = dataSetDESFinal[i - 1] + dataSetTrendDESFinal[i - 1];
                    dataSetForecastDESFinal.Add(forecastPointDES);
                }
    
            }
            var resultsDES = new Tuple<Tuple<double, double, double>, List<double>>(alphaErrorDESFinal, dataSetForecastDESFinal);
            return resultsDES;
        }
    }

}
