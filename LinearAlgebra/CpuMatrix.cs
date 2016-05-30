﻿using Icbld.BrightWire.Helper;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icbld.BrightWire.LinearAlgebra
{
    public class CpuMatrix : IIndexableMatrix
    {
        readonly Matrix<float> _matrix;

        public CpuMatrix(DenseMatrix matrix)
        {
            _matrix = matrix;
        }

        public CpuMatrix(Matrix<float> matrix)
        {
            _matrix = matrix;
        }

        protected virtual void Dispose(bool disposing)
        {
            // nop
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public float this[int row, int column]
        {
            get
            {
                return _matrix[row, column];
            }

            set
            {
                _matrix[row, column] = value;
            }
        }

        public int ColumnCount
        {
            get
            {
                return _matrix.ColumnCount;
            }
        }

        public int RowCount
        {
            get
            {
                return _matrix.RowCount;
            }
        }

        public object WrappedObject
        {
            get
            {
                return _matrix;
            }
        }

        public IVector Column(int index)
        {
            return new CpuVector(_matrix.Column(index));
        }

        public IIndexableMatrix Map(Func<float, float> mutator)
        {
            return new CpuMatrix(_matrix.Map(mutator));
        }

        public IIndexableMatrix MapIndexed(Func<int, int, float, float> mutator)
        {
            return new CpuMatrix(_matrix.MapIndexed(mutator));
        }

        public IMatrix Multiply(IMatrix matrix)
        {
            return new CpuMatrix(_matrix.Multiply((Matrix<float>)matrix.WrappedObject));
        }

        public IMatrix PointwiseMultiply(IMatrix matrix)
        {
            Debug.Assert(RowCount == matrix.RowCount && ColumnCount == matrix.ColumnCount);
            return new CpuMatrix(_matrix.PointwiseMultiply((Matrix<float>)matrix.WrappedObject));
        }

        public IVector RowSums(float coefficient = 1f)
        {
            var ret = _matrix.RowSums();
            if (coefficient != 1f)
                ret.MapInplace(v => v *= coefficient);
            return new CpuVector(ret);
        }

        public IVector ColumnSums(float coefficient = 1)
        {
            var ret = _matrix.ColumnSums();
            if (coefficient != 1f)
                ret.MapInplace(v => v *= coefficient);
            return new CpuVector(ret);
        }

        public IMatrix Add(IMatrix matrix)
        {
            return new CpuMatrix(_matrix.Add((Matrix<float>)matrix.WrappedObject));
        }

        public IMatrix Subtract(IMatrix matrix)
        {
            return new CpuMatrix(_matrix.Subtract((Matrix<float>)matrix.WrappedObject));
        }

        public IMatrix TransposeAndMultiply(IMatrix matrix)
        {
            return new CpuMatrix(_matrix.TransposeAndMultiply((Matrix<float>)matrix.WrappedObject));
        }

        public IMatrix TransposeThisAndMultiply(IMatrix matrix)
        {
            return new CpuMatrix(_matrix.TransposeThisAndMultiply((Matrix<float>)matrix.WrappedObject));
        }

        public IMatrix Transpose()
        {
            return new CpuMatrix(_matrix.Transpose());
        }

        public void Multiply(float scalar)
        {
            _matrix.MapInplace(v => v * scalar);
        }

        public override string ToString()
        {
            return _matrix.ToMatrixString();
        }

        public void AddInPlace(IMatrix matrix, float coefficient1 = 1.0f, float coefficient2 = 1.0f)
        {
            Debug.Assert(RowCount == matrix.RowCount && ColumnCount == matrix.ColumnCount);
            var other = (Matrix<float>)matrix.WrappedObject;
            _matrix.MapIndexedInplace((i, j, v) => (v * coefficient1) + (other[i, j] * coefficient2));
        }

        public void SubtractInPlace(IMatrix matrix, float coefficient1 = 1.0f, float coefficient2 = 1.0f)
        {
            Debug.Assert(RowCount == matrix.RowCount && ColumnCount == matrix.ColumnCount);
            var other = (Matrix<float>)matrix.WrappedObject;
            _matrix.MapIndexedInplace((i, j, v) => (v * coefficient1) - (other[i, j] * coefficient2));
        }

        internal static float _Sigmoid(float val)
        {
            return BoundMath.Constrain(1.0f / (1.0f + BoundMath.Exp(-1.0f * val)));
        }

        internal static float _SigmoidDerivative(float val)
        {
            var score = _Sigmoid(val);
            return BoundMath.Constrain(score * (1.0f - score));
        }

        internal static float _Tanh(float val)
        {
            return Convert.ToSingle(Math.Tanh(val));
        }

        internal static float _TanhDerivative(float val)
        {
            return 1.0f - Convert.ToSingle(Math.Pow(_Tanh(val), 2));
        }

        internal static float _Relu(float val)
        {
            return (val <= 0) ? 0 : BoundMath.Constrain(val);
        }

        internal static float _ReluDerivative(float val)
        {
            return (val <= 0) ? 0f : 1;
        }

        internal static float _LeakyRelu(float val)
        {
            return (val <= 0) ? 0.01f * val : BoundMath.Constrain(val);
        }

        internal static float _LeakyReluDerivative(float val)
        {
            return (val <= 0) ? 0.01f : 1;
        }

        public IMatrix ReluActivation()
        {
            return new CpuMatrix(_matrix.Map(_Relu));
        }

        public IMatrix ReluDerivative()
        {
            return new CpuMatrix(_matrix.Map(_ReluDerivative));
        }

        public IMatrix LeakyReluActivation()
        {
            return new CpuMatrix(_matrix.Map(_LeakyRelu));
        }

        public IMatrix LeakyReluDerivative()
        {
            return new CpuMatrix(_matrix.Map(_LeakyReluDerivative));
        }

        public IMatrix SigmoidActivation()
        {
            return new CpuMatrix(_matrix.Map(_Sigmoid));
        }

        public IMatrix SigmoidDerivative()
        {
            return new CpuMatrix(_matrix.Map(_SigmoidDerivative));
        }

        public IMatrix TanhActivation()
        {
            return new CpuMatrix(_matrix.Map(_Tanh));
        }

        public IMatrix TanhDerivative()
        {
            return new CpuMatrix(_matrix.Map(_TanhDerivative));
        }

        public void AddToEachRow(IVector vector)
        {
            var other = (Vector<float>)vector.WrappedObject;
            _matrix.MapIndexedInplace((j, k, v) => v + other[k]);
        }

        public void AddToEachColumn(IVector vector)
        {
            var other = (Vector<float>)vector.WrappedObject;
            _matrix.MapIndexedInplace((j, k, v) => v + other[j]);
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(_matrix.RowCount);
            writer.Write(_matrix.ColumnCount);
            for (var i = 0; i < _matrix.RowCount; i++) {
                for (var j = 0; j < _matrix.ColumnCount; j++) {
                    writer.Write(_matrix[i, j]);
                }
            }
        }

        public void ReadFrom(BinaryReader reader)
        {
            var rowCount = reader.ReadInt32();
            var columnCount = reader.ReadInt32();
            for (var i = 0; i < rowCount; i++) {
                for (var j = 0; j < columnCount; j++) {
                    var val = reader.ReadSingle();
                    if (i < _matrix.RowCount && j < _matrix.ColumnCount)
                        _matrix[i, j] = val;
                }
            }
        }

        public IIndexableMatrix AsIndexable()
        {
            return this;
        }

        public IVector Row(int index)
        {
            return new CpuVector(_matrix.Row(index));
        }

        public IEnumerable<IIndexableVector> Rows
        {
            get
            {
                return _matrix.EnumerateRows().Select(v => new CpuVector(v));
            }
        }

        public IEnumerable<IIndexableVector> Columns
        {
            get
            {
                return _matrix.EnumerateColumns().Select(v => new CpuVector(v));
            }
        }

        public IEnumerable<float> Values
        {
            get
            {
                return _matrix.Enumerate();
            }
        }

        public IMatrix GetNewMatrixFromRows(int[] rowIndexes)
        {
            return new CpuMatrix(DenseMatrix.Create(rowIndexes.Length, ColumnCount, (x, y) => _matrix[rowIndexes[x], y]));
        }

        public IMatrix GetNewMatrixFromColumns(int[] columnIndexes)
        {
            return new CpuMatrix(DenseMatrix.Create(RowCount, columnIndexes.Length, (x, y) => _matrix[x, columnIndexes[y]]));
        }

        public void ClearRows(int[] indexes)
        {
            _matrix.ClearRows(indexes);
        }

        public void ClearColumns(int[] indexes)
        {
            _matrix.ClearColumns(indexes);
        }

        public IMatrix Clone()
        {
            return new CpuMatrix(DenseMatrix.OfMatrix(_matrix));
        }

        public void Clear()
        {
            _matrix.Clear();
        }

        public IMatrix ConcatColumns(IMatrix bottom)
        {
            var t = this;
            var b = (CpuMatrix)bottom;
            Debug.Assert(ColumnCount == bottom.ColumnCount);

            var ret = DenseMatrix.Create(t.RowCount + b.RowCount, t.ColumnCount, (x, y) => {
                var m = x >= t.RowCount ? b._matrix : t._matrix;
                return m[x >= t.RowCount ? x - t.RowCount : x, y];
            });
            return new CpuMatrix(ret);
        }

        public IMatrix ConcatRows(IMatrix right)
        {
            var t = this;
            var b = (CpuMatrix)right;
            Debug.Assert(RowCount == right.RowCount);

            var ret = DenseMatrix.Create(t.RowCount, t.ColumnCount + b.ColumnCount, (x, y) => {
                var m = y >= t.ColumnCount ? b._matrix : t._matrix;
                return m[x, y >= t.ColumnCount ? y - t.ColumnCount : y];
            });
            return new CpuMatrix(ret);
        }

        public Tuple<IMatrix, IMatrix> SplitRows(int position)
        {
            var ret1 = DenseMatrix.Create(RowCount, position, (x, y) => this[x, y]);
            var ret2 = DenseMatrix.Create(RowCount, ColumnCount - position, (x, y) => this[x, position + y]);
            return Tuple.Create<IMatrix, IMatrix>(new CpuMatrix(ret1), new CpuMatrix(ret2));
        }

        public Tuple<IMatrix, IMatrix> SplitColumns(int position)
        {
            var ret1 = DenseMatrix.Create(position, ColumnCount, (x, y) => this[x, y]);
            var ret2 = DenseMatrix.Create(RowCount - position, ColumnCount, (x, y) => this[position + x, y]);
            return Tuple.Create<IMatrix, IMatrix>(new CpuMatrix(ret1), new CpuMatrix(ret2));
        }

        public IMatrix Sqrt(float valueAdjustment = 0)
        {
            return new CpuMatrix((DenseMatrix)_matrix.Map(v => {
                return Convert.ToSingle(Math.Sqrt(v + valueAdjustment));
            }));
        }

        public IMatrix PointwiseDivide(IMatrix matrix)
        {
            return new CpuMatrix(_matrix.PointwiseDivide((Matrix<float>)matrix.WrappedObject));
        }

        public void L1Regularisation(float coefficient)
        {
            _matrix.MapInplace(v => v - ((v > 0 ? 1 : v < 0 ? -1 : 0) * coefficient));
        }

        public void Constrain(float min, float max)
        {
            _matrix.MapInplace(v => v < min ? min : v > max ? max : v);
        }

        public IVector ColumnL2Norm()
        {
            var ret = _matrix.ColumnNorms(2.0);
            return new CpuVector(DenseVector.Create(ret.Count, i => Convert.ToSingle(ret[i])));
        }

        public IVector RowL2Norm()
        {
            var ret = _matrix.RowNorms(2.0);
            return new CpuVector(DenseVector.Create(ret.Count, i => Convert.ToSingle(ret[i])));
        }

        public void PointwiseDivideRows(IVector vector)
        {
            var v2 = vector.AsIndexable();
            _matrix.MapIndexedInplace((x, y, v) => v /= v2[x]);
        }

        public void PointwiseDivideColumns(IVector vector)
        {
            var v2 = vector.AsIndexable();
            _matrix.MapIndexedInplace((x, y, v) => v /= v2[y]);
        }

        public IVector Diagonal()
        {
            return new CpuVector((DenseVector)_matrix.Diagonal());
        }

        public IMatrix Pow(float power)
        {
            return new CpuMatrix(_matrix.Map(v => Convert.ToSingle(Math.Pow(v, power))));
        }

        public void Normalise(MatrixGrouping group, NormalisationType type)
        {
            if (type == NormalisationType.FeatureScale) {
                IEnumerable<Vector<float>> list = (group == MatrixGrouping.ByRow) ? _matrix.EnumerateRows() : _matrix.EnumerateColumns();
                var norm = list.Select(row => {
                    float min = 0f, max = 0f;
                    foreach (var val in row.Enumerate(Zeros.AllowSkip)) {
                        if (val > max)
                            max = val;
                        if (val < min)
                            min = val;
                    }
                    float range = max - min;
                    return Tuple.Create(min, range);
                }).ToList();

                if (group == MatrixGrouping.ByRow)
                    _matrix.MapIndexedInplace((x, y, v) => norm[x].Item2 > 0 ? (v - norm[x].Item1) / norm[x].Item2 : v);
                else
                    _matrix.MapIndexedInplace((x, y, v) => norm[y].Item2 > 0 ? (v - norm[y].Item1) / norm[y].Item2 : v);
            }
            else if(type == NormalisationType.Standard) {
                IEnumerable<Vector<float>> list = (group == MatrixGrouping.ByRow) ? _matrix.EnumerateRows() : _matrix.EnumerateColumns();
                var norm = list.Select(row => {
                    var mean = row.Average();
                    var stdDev = Convert.ToSingle(Math.Sqrt(row.Average(c => Math.Pow(c - mean, 2))));
                    return Tuple.Create(mean, stdDev);
                }).ToList();

                if(group == MatrixGrouping.ByRow)
                    _matrix.MapIndexedInplace((x, y, v) => norm[x].Item2 != 0 ? (v - norm[x].Item1) / norm[x].Item2 : v);
                else
                    _matrix.MapIndexedInplace((x, y, v) => norm[y].Item2 != 0 ? (v - norm[y].Item1) / norm[y].Item2 : v);
            }else if(type == NormalisationType.Euclidean || type == NormalisationType.Infinity || type == NormalisationType.Manhattan) {
                var p = (type == NormalisationType.Manhattan) ? 1.0 : (type == NormalisationType.Manhattan) ? 2.0 : double.PositiveInfinity;
                var norm = (group == MatrixGrouping.ByColumn) ? _matrix.NormalizeColumns(p) : _matrix.NormalizeRows(p);
                norm.CopyTo(_matrix);
            }
        }

        public void UpdateRow(int index, IIndexableVector vector, int columnIndex)
        {
            for(var i = 0; i < vector.Count; i++)
                _matrix[index, columnIndex + i] = vector[i];
        }

        public void UpdateColumn(int index, IIndexableVector vector, int rowIndex)
        {
            for (var i = 0; i < vector.Count; i++)
                _matrix[rowIndex + i, index] = vector[i];
        }

        public IVector GetRowSegment(int index, int columnIndex, int length)
        {
            var buffer = new float[length];
            for (var i = 0; i < length; i++)
                buffer[i] = _matrix[index, columnIndex + i];
            return new CpuVector(DenseVector.OfArray(buffer));
        }

        public IVector GetColumnSegment(int index, int rowIndex, int length)
        {
            var buffer = new float[length];
            for (var i = 0; i < length; i++)
                buffer[i] = _matrix[rowIndex + i, index];
            return new CpuVector(DenseVector.OfArray(buffer));
        }
    }
}
