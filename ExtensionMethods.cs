﻿using BrightWire.Bayesian;
using BrightWire.Bayesian.Training;
using BrightWire.DimensionalityReduction;
using BrightWire.ErrorMetrics;
using BrightWire.Helper;
using BrightWire.Linear;
using BrightWire.Linear.Training;
using BrightWire.Models;
using BrightWire.TabularData.Helper;
using BrightWire.TreeBased.Training;
using BrightWire.Unsupervised.Clustering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BrightWire
{
    /// <summary>
    /// Static extension methods
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Shuffles the enumerable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="seq">The sequence to shuffle</param>
        /// <param name="randomSeed">The random seed or null initialise randomlu</param>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> seq, int? randomSeed = null)
        {
            var rnd = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
            return seq.OrderBy(e => rnd.Next()).ToList();
        }

        /// <summary>
        /// Bags (select with replacement) the input sequence
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The input sequence</param>
        /// <param name="count">The size of the output sequence</param>
        /// <param name="randomSeed">The random seed or null initialise randomlu</param>
        /// <returns></returns>
        public static IReadOnlyList<T> Bag<T>(this IReadOnlyList<T> list, int count, int? randomSeed = null)
        {
            var rnd = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
            return Enumerable.Range(0, count)
                .Select(i => list[rnd.Next(0, list.Count)])
                .ToList()
            ;
        }

        /// <summary>
        /// Creates an error metric provider from an error metric descriptor
        /// </summary>
        /// <param name="type">The type of error metric</param>
        /// <returns></returns>
        public static IErrorMetric Create(this ErrorMetricType type)
        {
            switch(type) {
                case ErrorMetricType.OneHot:
                    return new OneHot();
                case ErrorMetricType.RMSE:
                    return new RMSE();
                case ErrorMetricType.BinaryClassification:
                    return new BinaryClassification();
                case ErrorMetricType.CrossEntropy:
                    return new CrossEntropy();
                case ErrorMetricType.Quadratic:
                    return new Quadratic();
                case ErrorMetricType.None:
                    return null;
            }
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calculates the distance between two vectors
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="vector1"></param>
        /// <param name="vector2"></param>
        public static float Calculate(this DistanceMetric distance, IVector vector1, IVector vector2)
        {
            switch (distance) {
                case DistanceMetric.Cosine:
                    return vector1.CosineDistance(vector2);
                case DistanceMetric.Euclidean:
                    return vector1.EuclideanDistance(vector2);
                case DistanceMetric.Manhattan:
                    return vector1.ManhattanDistance(vector2);
                case DistanceMetric.SquaredEuclidean:
                    return vector1.SquaredEuclidean(vector2);
                default:
                    return vector1.MeanSquaredDistance(vector2);
            }
        }

        /// <summary>
        /// Calculates the distance between two matrices
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="matrix1"></param>
        /// <param name="matrix2"></param>
        /// <returns></returns>
        public static IVector Calculate(this DistanceMetric distance, IMatrix matrix1, IMatrix matrix2)
        {
            switch (distance) {
                case DistanceMetric.Euclidean:
                    using (var diff = matrix1.Subtract(matrix2))
                    using (var diffSquared = diff.PointwiseMultiply(diff))
                    using (var rowSums = diffSquared.RowSums()) {
                        return rowSums.Sqrt();
                    }
                case DistanceMetric.SquaredEuclidean:
                    using (var diff = matrix1.Subtract(matrix2))
                    using (var diffSquared = diff.PointwiseMultiply(diff)) {
                        return diffSquared.RowSums();
                    }
                case DistanceMetric.Cosine:
                case DistanceMetric.Manhattan:
                case DistanceMetric.MeanSquared:
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Writes a matrix to an XmlWriter
        /// </summary>
        /// <param name="matrix">The matrix to write</param>
        /// <param name="writer"></param>
        /// <param name="name">The name to give the matrix</param>
        public static void WriteTo(this IMatrix matrix, XmlWriter writer, string name = null)
        {
            writer.WriteStartElement(name ?? "matrix");
            foreach (var item in matrix.Data)
                item.WriteTo("row", writer);
            writer.WriteEndElement();
        }

        /// <summary>
        /// Linear regression fits a line to a set of data that allows you predict future values
        /// </summary>
        /// <param name="table">The training data table</param>
        /// <param name="lap">Linear algebra provider</param>
        /// <returns>A trainer that can be used to build a linear regression model</returns>
        public static ILinearRegressionTrainer CreateLinearRegressionTrainer(this IDataTable table, ILinearAlgebraProvider lap)
        {
            return new RegressionTrainer(lap, table);
        }

        /// <summary>
        /// Logistic regression learns a sigmoid function over a set of data that learns to classify future values into positive or negative samples
        /// </summary>
        /// <param name="table">The training data provider</param>
        /// <param name="lap">Linear algebra provider</param>
        /// <returns>A trainer that can be used to build a logistic regression model</returns>
        public static ILogisticRegressionTrainer CreateLogisticRegressionTrainer(this IDataTable table, ILinearAlgebraProvider lap)
        {
            return new LogisticRegressionTrainer(lap, table);
        }

        /// <summary>
        /// Naive bayes is a classifier that assumes conditional independence between all features
        /// </summary>
        /// <param name="table">The training data provider</param>
        /// <returns>A naive bayes model</returns>
        public static NaiveBayes TrainNaiveBayes(this IDataTable table)
        {
            return NaiveBayesTrainer.Train(table);
        }

        /// <summary>
        /// Random projections allow you to reduce the dimensions of a matrix while still preserving significant information
        /// </summary>
        /// <param name="lap">Linear algebra provider</param>
        /// <param name="fixedSize">The size to reduce from</param>
        /// <param name="reducedSize">The size to reduce to</param>
        /// <param name="s"></param>
        public static IRandomProjection CreateRandomProjection(this ILinearAlgebraProvider lap, int fixedSize, int reducedSize, int s = 3)
        {
            return new RandomProjection(lap, fixedSize, reducedSize, s);
        }

        /// <summary>
        /// Markov models summarise sequential data (over a window of size 2)
        /// </summary>
        /// <typeparam name="T">The data type within the model</typeparam>
        /// <param name="data">An enumerable of sequences of type T</param>
        /// <returns>A sequence of markov model observations</returns>
        public static IEnumerable<MarkovModelObservation2<T>> TrainMarkovModel2<T>(this IEnumerable<IEnumerable<T>> data)
        {
            var trainer = new MarkovModelTrainer2<T>();
            foreach (var sequence in data)
                trainer.Add(sequence);
            return trainer.All;
        }

        /// <summary>
        /// Markov models summarise sequential data (over a window of size 3)
        /// </summary>
        /// <typeparam name="T">The data type within the model</typeparam>
        /// <param name="data">An enumerable of sequences of type T</param>
        /// <returns>A sequence of markov model observations</returns>
        public static IEnumerable<MarkovModelObservation3<T>> TrainMarkovModel3<T>(this IEnumerable<IEnumerable<T>> data)
        {
            var trainer = new MarkovModelTrainer3<T>();
            foreach (var sequence in data)
                trainer.Add(sequence);
            return trainer.All;
        }

        /// <summary>
        /// Bernoulli naive bayes treats each feature as either 1 or 0 - all feature counts are discarded. Useful for short documents.
        /// </summary>
        /// <param name="data">The training data</param>
        /// <returns>A model that can be used for classification</returns>
        public static BernoulliNaiveBayes TrainBernoulliNaiveBayes(this ClassificationBag data)
        {
            var trainer = new BernoulliNaiveBayesTrainer();
            foreach(var classification in data.Classifications)
                trainer.AddClassification(classification.Name, classification.Data);
            return trainer.Train();
        }

        /// <summary>
        /// Multinomial naive bayes preserves the count of each feature within the model. Useful for long documents.
        /// </summary>
        /// <param name="data">The training data</param>
        /// <returns>A model that can be used for classification</returns>
        public static MultinomialNaiveBayes TrainMultinomicalNaiveBayes(this ClassificationBag data)
        {
            var trainer = new MultinomialNaiveBayesTrainer();
            foreach (var classification in data.Classifications)
                trainer.AddClassification(classification.Name, classification.Data);
            return trainer.Train();
        }

        /// <summary>
        /// Decision trees build a logical tree to classify data. Various measures can be specified to prevent overfitting.
        /// </summary>
        /// <param name="data">The training data</param>
        /// <param name="minDataPerNode">Minimum number of data points per node to continue splitting</param>
        /// <param name="maxDepth">The maximum depth of each leaf</param>
        /// <param name="minInformationGain">The minimum information gain to continue splitting</param>
        /// <returns>A model that can be used for classification</returns>
        public static DecisionTree TrainDecisionTree(this IDataTable data, int? minDataPerNode = null, int? maxDepth = null, double? minInformationGain = null)
        {
            var config = new DecisionTreeTrainer.Config {
                MinDataPerNode = minDataPerNode,
                MaxDepth = maxDepth,
                MinInformationGain = minInformationGain
            };
            return DecisionTreeTrainer.Train(data, config);
        }

        /// <summary>
        /// Random forests are built on a bagged collection of features to try to capture the most salient points of the training data without overfitting
        /// </summary>
        /// <param name="data">The training data</param>
        /// <param name="b">The number of trees in the forest</param>
        /// <returns>A model that can be used for classification</returns>
        public static RandomForest TrainRandomForest(this IDataTable data, int b = 100)
        {
            return RandomForestTrainer.Train(data, b);
        }

        /// <summary>
        /// K Means uses coordinate descent and the euclidean distance between randomly selected centroids to cluster the data
        /// </summary>
        /// <param name="data">The list of vectors to cluster</param>
        /// <param name="k">The number of clusters to find</param>
        /// <param name="maxIterations">The maximum number of iterations</param>
        /// <returns>A list of k clusters</returns>
        public static IReadOnlyList<IReadOnlyList<IVector>> KMeans(this IReadOnlyList<IVector> data, int k, int maxIterations = 1000)
        {
            using(var clusterer = new KMeans(k, data, DistanceMetric.Euclidean)) {
                clusterer.ClusterUntilConverged(maxIterations);
                return clusterer.Clusters;
            }
        }

        /// <summary>
        /// Non negative matrix factorisation - clustering based on the factorisation of non-negative matrices. Only applicable for training data that is non-negative.
        /// </summary>
        /// <param name="data">The training data</param>
        /// <param name="lap">Linear alegbra provider</param>
        /// <param name="k">The number of clusters</param>
        /// <param name="maxIterations">The maximum number of iterations</param>
        /// <returns>A list of k clusters</returns>
        public static IReadOnlyList<IReadOnlyList<IVector>> NNMF(this IReadOnlyList<IVector> data, ILinearAlgebraProvider lap, int k, int maxIterations = 1000)
        {
            var clusterer = new NonNegativeMatrixFactorisation(lap, k);
            return clusterer.Cluster(data, maxIterations);
        }

        /// <summary>
        /// Parses a CSV file into a data table
        /// </summary>
        /// <param name="streamReader">The stream of CSV data</param>
        /// <param name="delimeter">The CSV delimeter</param>
        /// <param name="hasHeader">True if there is a header</param>
        /// <param name="output">A stream to write the data table to (for file based processing) - null for in memory processing</param>
        public static IDataTable ParseCSV(this StreamReader streamReader, char delimeter = ',', bool? hasHeader = null, Stream output = null)
        {
            var builder = new CSVDataTableBuilder(delimeter);
            return builder.Parse(streamReader, output, hasHeader);
        }
    }
}
