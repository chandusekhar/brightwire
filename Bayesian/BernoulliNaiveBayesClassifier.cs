﻿using BrightWire.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrightWire.Bayesian
{
    internal class BernoulliNaiveBayesClassifier : IIndexBasedClassifier
    {
        class Classification
        {
            readonly BernoulliNaiveBayes.Class _class;
            readonly List<uint> _excluded;

            public Classification(uint[] vocabulary, BernoulliNaiveBayes.Class cls)
            {
                _class = cls;

                var included = new HashSet<uint>();
                foreach (var item in cls.Index)
                    included.Add(item.StringIndex);
                _excluded = vocabulary.Where(w => !included.Contains(w)).ToList();
            }
            public string Label { get { return _class.Label; } }
            public double GetScore(HashSet<uint> documentSet)
            {
                double ret = _class.Prior;

                foreach (var item in _class.Index) {
                    if (documentSet.Contains(item.StringIndex))
                        ret += item.ConditionalProbability;
                    else
                        ret += item.InverseProbability;
                }

                int missingCount = 0, inverseMissingCount = 0;
                foreach (var word in _excluded) {
                    if (documentSet.Contains(word))
                        ++missingCount;
                    else
                        ++inverseMissingCount;
                }
                ret += (missingCount * _class.MissingProbability) + (inverseMissingCount * _class.InverseMissingProbability);
                return ret;
            }
        }
        readonly List<Classification> _classification = new List<Classification>();

        public BernoulliNaiveBayesClassifier(BernoulliNaiveBayes model)
        {
            _classification = model.ClassData.Select(c => new Classification(model.Vocabulary, c)).ToList();
        }

        public IEnumerable<string> Classify(IReadOnlyList<uint> stringIndexList)
        {
            var documentSet = new HashSet<uint>(stringIndexList);
            var ret = new List<Tuple<string, double>>();
            foreach (var cls in _classification)
                ret.Add(Tuple.Create(cls.Label, cls.GetScore(documentSet)));
            return ret.OrderByDescending(kv => kv.Item2).Select(kv => kv.Item1);
        }
    }
}
