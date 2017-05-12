﻿using BrightWire.ExecutionGraph.Helper;
using BrightWire.ExecutionGraph.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightWire.ExecutionGraph.Activation
{
    class Relu : NodeBase
    {
        class Backpropagation : SingleBackpropagationBase
        {
            readonly IReadOnlyList<IMatrix> _input;
            readonly Relu _source;

            public Backpropagation(Relu source, IReadOnlyList<IMatrix> matrix)
            {
                _input = matrix;
                _source = source;
            }

            protected override void _Dispose(bool isDisposing)
            {
                foreach(var item in _input)
                    item.Dispose();
            }

            protected override IGraphData _Backward(IGraphData errorSignal, IContext context, IReadOnlyList<INode> parents)
            {
                return context.ToGraphData(_input.Zip(errorSignal.Decompose(), (input, es) => {
                    using (var od = input.ReluDerivative()) {
                        var delta = es.PointwiseMultiply(od);
                        //context.LearningContext.Log("relu-backpropagation", channel, _source.GetHashCode(), errorSignal, delta);
                        return delta;
                    }
                }));
            }
        }

        public Relu(string name = null) : base(name) { }

        public override void ExecuteForward(IContext context)
        {
            var input = context.Data.Decompose();
            var output = context.ToGraphData(input.Select(m => m.ReluActivation()));
            _AddNextGraphAction(context, output, () => new Backpropagation(this, input));
        }
    }
}
