using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECIS.Client.WEIS
{
    public class FunctionHelper
    {
        private static bool CompareContains(object[] obj1, object[] obj2)
        {
            List<bool> comparers = new List<bool>();
            foreach (var one in obj1)
            {
                if (obj2.Contains(one))
                    comparers.Add(true);
                else
                    comparers.Add(false);
            }
            if (comparers.Count() > 0)
            {
                if (comparers.Contains(true))
                    return true;
                else
                    return false;
            }
            else
            {
                return false;
            }
        }

        private static bool CompareEqual(object[] obj1, object[] obj2)
        {
            IStructuralEquatable se1 = obj1;
            return se1.Equals(obj2, StructuralComparisons.StructuralEqualityComparer);
            
        }

        /// <summary>
        /// put Operand to AND or OR
        /// </summary>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        /// <param name="operand"></param>
        /// <returns></returns>
        public static bool CompareArrays(object[] obj1, object[] obj2, string operand = "AND")
        {
            if (operand.Equals("AND"))
                return CompareEqual(obj1, obj2);
            else
                return CompareContains(obj1, obj2);
        }

        public static bool CompareBaseOP(object[] obj1, object[] obj2, string operand = "AND")
        {
            var isMatchBaseOP = false;
            var FilterBaseOP = obj1.ToArray();
            if (operand.ToLower() == "and")
            {
                List<bool> match = new List<bool>();
                foreach (var op in obj2)
                {
                    if (FilterBaseOP.Length == 1)
                    {
                        if (FilterBaseOP.FirstOrDefault().Equals(op))
                        {
                            isMatchBaseOP = true;
                            break;
                        }
                    }
                    else
                    {
                        foreach (var fltr in FilterBaseOP)
                        {
                            if (fltr.Equals(op))
                            {
                                match.Add(true);
                                break;
                                    
                            }
                        }
                    }
                }
                if (!isMatchBaseOP)                
                    if (match.Count == FilterBaseOP.Length)
                        isMatchBaseOP = true;
                
            }
            else if (operand.ToLower() == "not")
            {
                bool notmatch = false;
                foreach (var op in obj2)
                {
                    if (FilterBaseOP.Length == 1)
                    {
                        if (FilterBaseOP.FirstOrDefault().Equals(op))
                        {
                            isMatchBaseOP = false;
                            break;
                        }
                    }
                    else
                    {
                        foreach (var fltr in FilterBaseOP)
                        {
                            if (fltr.Equals(op))
                            {
                                notmatch = true;
                                break;

                            }
                        }
                    }
                    if (notmatch)
                    {
                        break;
                    }
                }
                if (!notmatch)
                    isMatchBaseOP = true;
            }
            else
            {
                //contains
                var match = false;
                foreach (var op in obj2)
                {

                    match = Array.Exists(FilterBaseOP, element => element.Equals(op));
                    if (match) {
                        isMatchBaseOP = true;
                        break;
                    }
                }
            }
            return isMatchBaseOP;
        }

        public static bool isMatchOP(object[] BasePhaseOP, object[] OPs, string opRelation)
        {
            var isMatchBaseOP = true;
            var BaseOP = BasePhaseOP.ToArray();
            if (opRelation.ToLower() == "and")
            {
                var match = true;
                foreach (var op in OPs)
                {
                    match = Array.Exists(BaseOP, element => element.Equals(op));
                    if (!match)
                    {
                        isMatchBaseOP = false;
                        break;
                    }
                }
            }
            else
            {
                //contains
                var match = false;
                foreach (var op in OPs)
                {
                    match = Array.Exists(BaseOP, element => element.Equals(op));
                    if (match) break;
                }
            }
            return isMatchBaseOP;
        }
    }
}
