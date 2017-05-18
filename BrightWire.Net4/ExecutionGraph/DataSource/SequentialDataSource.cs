﻿using BrightWire.ExecutionGraph.Helper;
using BrightWire.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrightWire.ExecutionGraph.DataSource
{
    class SequentialDataSource : IDataSource
    {
        readonly int[] _rowDepth;
        readonly int _inputSize, _outputSize;
        readonly IReadOnlyList<FloatMatrix> _data;
        readonly ILinearAlgebraProvider _lap;

        public SequentialDataSource(ILinearAlgebraProvider lap, IReadOnlyList<FloatMatrix> matrixList)
        {
            _lap = lap;
            _data = matrixList;
            _outputSize = -1;

            int index = 0;
            _rowDepth = new int[matrixList.Count];
            foreach (var item in matrixList) {
                if(index == 0)
                    _inputSize = item.ColumnCount;
                _rowDepth[index++] = item.RowCount;
            }
        }

        public bool IsSequential => true;
        public int InputSize => _inputSize;
        public int OutputSize => _outputSize;
        public int RowCount => _data.Count;

        public IMiniBatch Get(IReadOnlyList<int> rows)
        {
            var data = rows.Select(i => _data[i]).ToList();

            List<FloatVector> temp;
            var inputData = new Dictionary<int, List<FloatVector>>();
            foreach (var item in data) {
                var input = item;
                for (int i = 0, len = input.RowCount; i < len; i++) {
                    if (!inputData.TryGetValue(i, out temp))
                        inputData.Add(i, temp = new List<FloatVector>());
                    temp.Add(input.Row[i]);
                }
            }

            var miniBatch = new MiniBatch(rows, this);
            foreach (var item in inputData.OrderBy(kv => kv.Key)) {
                var input = _lap.CreateMatrix(item.Value);
                var type = (item.Key == 0)
                    ? MiniBatchType.SequenceStart
                    : item.Key == (inputData.Count - 1)
                        ? MiniBatchType.SequenceEnd
                        : MiniBatchType.Standard
                ;
                miniBatch.Add(type, input, null);
            }
            return miniBatch;
        }

        public IReadOnlyList<IReadOnlyList<int>> GetBuckets()
        {
            return _rowDepth
                .Select((r, i) => (r, i))
                .GroupBy(t => t.Item1)
                .Select(g => g.Select(d => d.Item2).ToList())
                .ToList()
            ;
        }

        public IDataSource CloneWith(IDataTable dataTable)
        {
            throw new NotImplementedException();
        }

        public void OnBatchProcessed(IContext context)
        {
            // nop
        }
    }
}
