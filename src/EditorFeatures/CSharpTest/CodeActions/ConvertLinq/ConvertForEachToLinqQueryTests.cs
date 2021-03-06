﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ConvertLinq
{
    public class ConvertForEachToLinqQueryTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery.CSharpConvertForEachToLinqQueryProvider();

        #region Query Expressions

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task QueryForForWhere()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class Query
{
    public IEnumerable<int> void Main(string[] args)
    {
        List<int> c1 = new List<int>{1, 2, 3, 4, 5, 7};
        List<int> c2 = new List<int>{10, 30, 40, 50, 60, 70};
        [|foreach (var x1 in c1)
        {
            foreach (var x2 in c2)
            {
                if (object.Equals(x1, x2 / 10))
                {
                    yield return x1 + x2;
                }
            }
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class Query
{
    public IEnumerable<int> void Main(string[] args)
    {
        List<int> c1 = new List<int>{1, 2, 3, 4, 5, 7};
        List<int> c2 = new List<int>{10, 30, 40, 50, 60, 70};
        return from x1 in c1
               from x2 in c2
               where object.Equals(x1, x2 / 10)
               select x1 + x2;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task QueryWithEscapedSymbols()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class Query
{
    public IEnumerable<int> void Main(string[] args)
    {
        List<int> c1 = new List<int>{1, 2, 3, 4, 5, 7};
        List<int> c2 = new List<int>{10, 30, 40, 50, 60, 70};
        [|foreach (var @object in c1)
        {
            foreach (var x2 in c2)
            {
                yield return @object + x2;
            }
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class Query
{
    public IEnumerable<int> void Main(string[] args)
    {
        List<int> c1 = new List<int>{1, 2, 3, 4, 5, 7};
        List<int> c2 = new List<int>{10, 30, 40, 50, 60, 70};
        return from @object in c1
               from x2 in c2
               select @object + x2;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task QueryForVarForWhere()
        {
            string source = @"
using System.Linq;

class C
{
    IEnumerable<int> M()
    {
        [|foreach (var num in new int[] { 1, 2 })
        {
            var n1 = num + 1;
            foreach (var a in new int[] { 5, 6 })
            {
                foreach (var x1 in new int[] { 3, 4 })
                {
                    if (object.Equals(num, x1))
                    {
                        foreach (var x2 in new int[] { 7, 8 })
                        {
                            if (object.Equals(num, x2))
                            {
                                var n2 = x2 - 1;
                                yield return n2 + n1;
                            }
                        }
                    }
                }
            }|]
        }
    }
}";
            string output = @"
using System.Linq;

class C
{
    IEnumerable<int> M()
    {
        return from num in new int[] { 1, 2 }
               let n1 = num + 1
               from a in new int[] { 5, 6 }
               from x1 in new int[] { 3, 4 }
               where object.Equals(num, x1)
               from x2 in new int[] { 7, 8 }
               where object.Equals(num, x2)
               let n2 = x2 - 1
               select n2 + n1;
    }
}";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task QueryLet()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class Query
{
    void M()
    {
        List<int> c1 = new List<int>{ 1, 2, 3 };
        List<int> r1 = new List<int>();
        [|foreach (int x in c1)
        {
            var g = x * 10;
            var z = g + x*100;
            var a = 5 + z;
            r1.Add(x + z - a);
        }|]
    }
}";
            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class Query
{
    void M()
    {
        List<int> c1 = new List<int>{ 1, 2, 3 };
        List<int> r1 = (from int x in c1
                        let g = x * 10
                        let z = g + x * 100
                        let a = 5 + z
                        select x + z - a).ToList();
    }
}";

            await TestInRegularAndScriptAsync(source, output);
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task QueryEmptyDeclarations()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class Query
{
    void M()
    {
        [|foreach (int x in new[] {1,2})
        {
            int a = 3, b, c = 1;
            if (x > c)
            {
                b = 0;
                Console.Write(a + x + b);
            }
        }|]
    }
}";

            await TestMissingInRegularAndScriptAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task QueryWhereClause()
        {
            string source = @"
using System;
using System.Linq;
class C
{
    IEnumerable<int> M()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        [|foreach (var x in nums)
        {
            if (x > 2)
            {
                yield return x;
            }
        }|]
    }
}";
            string output = @"
using System;
using System.Linq;
class C
{
    IEnumerable<int> M()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        return from x in nums
               where x > 2
               select x;
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task QueryOverQueries()
        {
            string source = @"
using System;
using System.Linq;
class C
{
    IEnumerable<int> M()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        [|foreach (var y in from x in nums select x)
        {
            foreach (var z in from x in nums select x)
            {
                yield return y;
            }
        }|]
    }
}";
            string output = @"
using System;
using System.Linq;
class C
{
    IEnumerable<int> M()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        return from y in
                   from x in nums select x
               from z in
                   from x in nums select x
               select y;
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task QueryNoVariablesUsed()
        {
            string source = @"
using System;
using System.Linq;
class C
{
    void M()
    {
        [|foreach (var a in new[] { 1 })
        {
            foreach (var b in new[] { 2 })
            {
                System.Console.Write(0);
            }
        }|]
    }
}";
            string output = @"
using System;
using System.Linq;
class C
{
    void M()
    {
        foreach (var _ in from a in new[] { 1 }
                          from b in new[] { 2 }
                          select new { })
        {
            System.Console.Write(0);
        }
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task QueryNoBlock()
        {
            string source = @"
using System;
using System.Linq;
class C
{
    void M()
    {
        [|foreach (var a in new[] { 1 })
            foreach (var b in new[] { 2 })
                System.Console.Write(a);|]
    }
}";
            string output = @"
using System;
using System.Linq;
class C
{
    void M()
    {
        foreach (var a in from a in new[] { 1 }
                          from b in new[] { 2 }
                          select a)
        {
            System.Console.Write(a);
        }
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task QuerySelectExpression()
        {
            string source = @"
using System;
using System.Linq;
class C
{
    void M()
    {
        [|foreach (var a in new[] { 1 })
            foreach (var b in new[] { 2 })
                Console.Write(a + b);|]
    }
}";
            string output = @"
using System;
using System.Linq;
class C
{
    void M()
    {
        foreach (var (a, b) in from a in new[] { 1 }
                               from b in new[] { 2 }
                               select (a, b))
        {
            Console.Write(a + b);
        }
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task QuerySelectMultipleExpressions()
        {
            string source = @"
using System;
using System.Linq;
class C
{
    void M()
    {
        [|foreach (var a in new[] { 1 })
            foreach (var b in new[] { 2 })
            {
                Console.Write(a + b);
                Console.Write(a * b);
            }|]
    }
}";
            string output = @"
using System;
using System.Linq;
class C
{
    void M()
    {
        foreach (var (a, b) in from a in new[] { 1 }
                               from b in new[] { 2 }
                               select (a, b))
        {
            Console.Write(a + b);
            Console.Write(a * b);
        }
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task EmptyBody()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
            }
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        foreach (var _ in from int n1 in nums
                          from int n2 in nums
                          select new { })
        {
        }
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task EmptyBodyNoBlock()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums);
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        foreach (var _ in from int n1 in nums
                          from int n2 in nums
                          select new { })
        {
        }
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task AddUsingToExistingList()
        {
            string source = @"
using System.Collections.Generic;
class C
{
    void M(IEnumerable<int> nums)
    {
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums);
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;

class C
{
    void M(IEnumerable<int> nums)
    {
        foreach (var _ in from int n1 in nums
                          from int n2 in nums
                          select new { })
        {
        }
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task AddFirstUsing()
        {
            string source = @"
class C
{
    void M(int[] nums)
    {
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums);
        }|]
    }
}
";
            string output = @"using System.Linq;

class C
{
    void M(int[] nums)
    {
        foreach (var _ in from int n1 in nums
                          from int n2 in nums
                          select new { })
        {
        }
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task EmptyBodyDeclarationAsLast()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                var a = n1 + n2;
            }
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        foreach (var _ in from int n1 in nums
                          from int n2 in nums
                          let a = n1 + n2
                          select new { })
        {
        }
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task EmptyBodyMultipleDeclarationsAsLast()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                int a = n1 + n2, b = n1 * n2;
            }
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        foreach (var _ in from int n1 in nums
                          from int n2 in nums
                          let a = n1 + n2
                          let b = n1 * n2
                          select new { })
        {
        }
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        #endregion

        #region Assignments, Declarations, Returns

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ReturnInvocationAndYieldReturn()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                yield return N(n1);
            }
        }|]
    }

    int N(int n) => n;
}
";
            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        return from int n1 in nums
               from int n2 in nums
               select N(n1);
    }

    int N(int n) => n;
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task BlockBodiedProperty()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    private readonly int[] _nums = new int[] { 1, 2, 3, 4 };
    public IEnumerable<int> Query1 { get { [|foreach (var x in _nums) { yield return x + 1; }|] } }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    private readonly int[] _nums = new int[] { 1, 2, 3, 4 };
    public IEnumerable<int> Query1 { get { return from x in _nums select x + 1; } }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ReturnIEnumerable()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                yield return n1;
            }
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        return from int n1 in nums
               from int n2 in nums
               select n1;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ReturnIEnumerableWithYieldReturnAndLocalFunction()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<IEnumerable<int>> M(IEnumerable<int> nums)
    {
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                yield return f(n1);
            }
        }|]

        yield break;

        IEnumerable<int> f(int a)
        {
            yield return a;
        }
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<IEnumerable<int>> M(IEnumerable<int> nums)
    {
        return from int n1 in nums
               from int n2 in nums
               select f(n1);
        IEnumerable<int> f(int a)
        {
            yield return a;
        }
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ReturnIEnumerablePartialMethod()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
partial class C
{
    partial IEnumerable<int> M(IEnumerable<int> nums);
}
partial class C
{
    partial IEnumerable<int> M(IEnumerable<int> nums)
    {
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                yield return n1;
            }
        }|]

        yield break;
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
partial class C
{
    partial IEnumerable<int> M(IEnumerable<int> nums);
}
partial class C
{
    partial IEnumerable<int> M(IEnumerable<int> nums)
    {
        return from int n1 in nums
               from int n2 in nums
               select n1;
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        #endregion

        #region In foreach

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task QueryInForEachWithSameVariableNameAndDifferentType()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class A
{
    public static implicit operator int(A x)
    {
        throw null;
    }

    public static implicit operator A(int x)
    {
        throw null;
    }
}
class B : A { }
class C
{
    void M(IEnumerable<int> nums)
    {
        [|foreach (B a in nums)
        {
            foreach (A c in nums)
            {
                Console.Write(a.ToString());
            }
        }|]
    }
}
";
            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class A
{
    public static implicit operator int(A x)
    {
        throw null;
    }

    public static implicit operator A(int x)
    {
        throw null;
    }
}
class B : A { }
class C
{
    void M(IEnumerable<int> nums)
    {
        foreach (var a in from B a in nums
                          from A c in nums
                          select a)
        {
            Console.Write(a.ToString());
        }
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task QueryInForEachWithSameVariableNameAndSameType()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class A
{
    public static implicit operator int(A x)
    {
        throw null;
    }

    public static implicit operator A(int x)
    {
        throw null;
    }
}
class C
{
    void M(IEnumerable<int> nums)
    {
        [|foreach (A a in nums)
        {
            foreach (A c in nums)
            {
                Console.Write(a.ToString());
            }
        }|]
    }
}
";
            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class A
{
    public static implicit operator int(A x)
    {
        throw null;
    }

    public static implicit operator A(int x)
    {
        throw null;
    }
}
class C
{
    void M(IEnumerable<int> nums)
    {
        foreach (var a in from A a in nums
                          from A c in nums
                          select a)
        {
            Console.Write(a.ToString());
        }
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task QueryInForEachWithConvertedType()
        {
            string source = @"
using System;
using System.Collections.Generic;

static class Extensions
{
    public static IEnumerable<C> Select(this int[] x, Func<int, C> predicate) => throw null;
}

class C
{
    public static implicit operator int(C x)
    {
        throw null;
    }

    public static implicit operator C(int x)
    {
        throw null;
    }

    IEnumerable<C> Test()
    {
        [|foreach (var x in new[] { 1, 2, 3 })
        {
            yield return x;
        }|]
    }
}
";
            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;

static class Extensions
{
    public static IEnumerable<C> Select(this int[] x, Func<int, C> predicate) => throw null;
}

class C
{
    public static implicit operator int(C x)
    {
        throw null;
    }

    public static implicit operator C(int x)
    {
        throw null;
    }

    IEnumerable<C> Test()
    {
        return from x in new[] { 1, 2, 3 }
               select x;
    }
}
";
            await TestAsync(source, output, parseOptions: null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task IQueryableConvertedToIEnumerableInReturn()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        [|foreach (int n1 in nums.AsQueryable())
        {
            yield return n1;
        }|]

        yield break;
    }
}";
            string output = @"
using System.Collections.Generic;
using System.Linq;

class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        return from int n1 in nums.AsQueryable()
               select n1;
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ReturnIQueryableConvertedToIEnumerableInAssignment()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        [|foreach (int n1 in nums.AsQueryable())
        {
            yield return n1;
        }|]
    }
}";
            string output = @"
using System.Collections.Generic;
using System.Linq;

class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        return from int n1 in nums.AsQueryable()
               select n1;
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        #endregion

        #region In ToList

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ToListLastDeclarationMerge()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        List<int> list0 = new List<int>(), list = new List<int>();
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                list.Add(n1);
            }
        }|]

        return list;
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        List<int> list0 = new List<int>();
        return (from int n1 in nums
                from int n2 in nums
                select n1).ToList();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ToListParametrizedConstructor()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        List<int> list = new List<int>(nums);
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                list.Add(n1);
            }
        }|]

        return list;
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        List<int> list = new List<int>(nums);
        list.AddRange(from int n1 in nums
                      from int n2 in nums
                      select n1);
        return list;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ToListWithListInitializer()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        List<int> list = new List<int>() { 1, 2, 3 };
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                list.Add(n1);
            }
        }|]

        return list;
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        List<int> list = new List<int>() { 1, 2, 3 };
        list.AddRange(from int n1 in nums
                      from int n2 in nums
                      select n1);
        return list;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ToListWithEmptyArgumentList()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        List<int> list = new List<int> { };
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                list.Add(n1);
            }
        }|]

        return list;
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        return (from int n1 in nums
                from int n2 in nums
                select n1).ToList();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ToListNotLastDeclaration()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        List<int> list = new List<int>(), list1 = new List<int>();
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                list.Add(n1);
            }
        }|]

        return list;
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        List<int> list = new List<int>(), list1 = new List<int>();
        list.AddRange(from int n1 in nums
                      from int n2 in nums
                      select n1);
        return list;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ToListAssignToParameter()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums, List<int> list)
    {
        list = new List<int>();
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                list.Add(n1);
            }
        }|]

        return list;
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums, List<int> list)
    {
        list = (from int n1 in nums
                from int n2 in nums
                select n1).ToList();
        return list;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ToListToArrayElement()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums, List<int>[] lists)
    {
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                lists[0].Add(n1);
            }
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums, List<int>[] lists)
    {
        lists[0].AddRange(from int n1 in nums
                          from int n2 in nums
                          select n1);
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ToListToNewArrayElement()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums, List<int>[] lists)
    {
        lists[0] = new List<int>();
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                lists[0].Add(n1);
            }
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums, List<int>[] lists)
    {
        lists[0] = (from int n1 in nums
                    from int n2 in nums
                    select n1).ToList();
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ToListHashSetNoConversion()
        {
            string source = @"
using System.Collections.Generic;
class C
{
    void M(IEnumerable<int> nums)
    {
        var hashSet = new HashSet<int>();
        [|foreach (int n1 in nums)
        {
            hashSet.Add(n1);
        }|]
    }
}
";
            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ToListMergeWithReturn()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        var list = new List<int>();
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                list.Add(n1);
            }
        }|]

        return list;
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        return (from int n1 in nums
                from int n2 in nums
                select n1).ToList();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ToListSeparateDeclarationAndAssignmentMergeWithReturn()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        List<int> list;
        list = new List<int>();
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                list.Add(n1);
            }
        }|]

        return list;
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        List<int> list;
        return (from int n1 in nums
                from int n2 in nums
                select n1).ToList();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ToListSeparateDeclarationAndAssignment()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        List<int> list;
        list = new List<int>();
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                list.Add(n1);
            }
        }|]

        return list.Count;
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        List<int> list;
        list = (from int n1 in nums
                from int n2 in nums
                select n1).ToList();
        return list.Count;
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ToListTypeReplacement01()
        {
            string source = @"
using System;
using System.Linq;
using C = System.Collections.Generic.List<int>;
class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C { 1, 2, 3 };
        C c2 = new C { 10, 20, 30 };
        C c3 = new C { 100, 200, 300 };
        C r1 = new C();
        [|foreach (int x in c1)
        {
            foreach (int y in c2)
            {
                foreach (int z in c3)
                {
                    var g = x + y + z;
                    if (x + y / 10 + z / 100 < 6)
                    {
                        r1.Add(g);
                    }
                }
            }
        }|]

        Console.WriteLine(r1);
    }
}
";
            string output = @"
using System;
using System.Linq;
using C = System.Collections.Generic.List<int>;
class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C { 1, 2, 3 };
        C c2 = new C { 10, 20, 30 };
        C c3 = new C { 100, 200, 300 };
        C r1 = (from int x in c1
                from int y in c2
                from int z in c3
                let g = x + y + z
                where x + y / 10 + z / 100 < 6
                select g).ToList();
        Console.WriteLine(r1);
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ToListTypeReplacement02()
        {
            string source = @"
using System.Linq;
using System;
using C = System.Collections.Generic.List<int>;
class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C { 1, 2, 3 };
        C c2 = new C { 10, 20, 30 };
        C r1 = new C();
        [|foreach (int x in c1)
        {
            foreach (var y in c2)
            {
                if (Equals(x, y / 10))
                {
                    var z = x + y;
                    r1.Add(z);
                }
            }
        }|]

        Console.WriteLine(r1);
    }
}
";
            string output = @"
using System.Linq;
using System;
using C = System.Collections.Generic.List<int>;
class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C { 1, 2, 3 };
        C c2 = new C { 10, 20, 30 };
        C r1 = (from int x in c1
                from y in c2
                where Equals(x, y / 10)
                let z = x + y
                select z).ToList();
        Console.WriteLine(r1);
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ToListPropertyAssignment()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        var c = new C();
        c.A = new List<int>();
        [|foreach (var x in nums)
        {
            c.A.Add(x + 1);
        }|]
    }

    class C
    {
        public List<int> A { get; set; }
    }
}";
            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        var c = new C();
        c.A = (from x in nums
               select x + 1).ToList();
    }

    class C
    {
        public List<int> A { get; set; }
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ToListPropertyAssignmentNoDeclaration()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    void M()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        var c = new C();
        [|foreach (var x in nums)
        {
            c.A.Add(x + 1);
        }|]
    }

    class C
    {
        public List<int> A { get; set; }
    }
}";
            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    void M()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        var c = new C();
        c.A.AddRange(from x in nums
                     select x + 1);
    }

    class C
    {
        public List<int> A { get; set; }
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ToListNoInitialization()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    public List<int> A { get; set; }

    void M()
    {
        [|foreach (var x in new int[] { 1, 2, 3, 4 })
        {
            A.Add(x + 1);
        }|]
    }
}";
            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    public List<int> A { get; set; }

    void M()
    {
        A.AddRange(from x in new int[] { 1, 2, 3, 4 }
                   select x + 1);
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task ToListOverride()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

public static class C
{ 
   public static void Add<T>(this List<T> list, T value, T anotherValue) { }
}
public class Test
{
    void M()
    {
        var list = new List<int>();
        [|foreach (var x in new int[] { 1, 2, 3, 4 })
        {
            list.Add(x + 1, x);
        }|]
    }
}";
            await TestMissingAsync(source);
        }

        #endregion

        #region In Count

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountInMultipleDeclarationLast()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        int i = 0, cnt = 0;
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                cnt++;
            }
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        int i = 0, cnt = (from int n1 in nums
                          from int n2 in nums
                          select n1).Count();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountInMultipleDeclarationNotLast()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        int cnt = 0, i = 0;
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                cnt++;
            }
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        int cnt = 0, i = 0;
        cnt += (from int n1 in nums
                from int n2 in nums
                select n1).Count();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountInParameter()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums, int c)
    {
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                c++;
            }
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums, int c)
    {
        c += (from int n1 in nums
              from int n2 in nums
              select n1).Count();
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountInParameterAssignedToZero()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums, int c)
    {
        c = 0;
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                c++;
            }
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums, int c)
    {
        c = (from int n1 in nums
             from int n2 in nums
             select n1).Count();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountInParameterAssignedToNonZero()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums, int c)
    {
        c = 5;
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                c++;
            }
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums, int c)
    {
        c = 5;
        c += (from int n1 in nums
              from int n2 in nums
              select n1).Count();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountInDeclarationMergeToReturn()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        var cnt = 0;
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                cnt++;
            }
        }|]

        return cnt;
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        return (from int n1 in nums
                from int n2 in nums
                select n1).Count();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountInDeclarationConversion()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    double M(IEnumerable<int> nums)
    {
        double c = 0;
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                c++;
            }
        }|]

        return c;
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    double M(IEnumerable<int> nums)
    {
        return (from int n1 in nums
                from int n2 in nums
                select n1).Count();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountInMultipleDeclarationMergeToReturnLast()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        int c = 0, cnt = 0;
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                cnt++;
            }
        }|]

        return cnt;
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        int c = 0;
        return (from int n1 in nums
                from int n2 in nums
                select n1).Count();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountInMultipleDeclarationLastButNotZero()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        int c = 0, cnt = 5;
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                cnt++;
            }
        }|]

        return cnt;
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        int c = 0, cnt = 5;
        cnt += (from int n1 in nums
                from int n2 in nums
                select n1).Count();
        return cnt;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountInMultipleDeclarationMergeToReturnNotLast()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        int cnt = 0, c = 0;
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                cnt++;
            }
        }|]

        return cnt;
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        int cnt = 0, c = 0;
        cnt += (from int n1 in nums
                from int n2 in nums
                select n1).Count();
        return cnt;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountInMultipleDeclarationNonZeroToReturnNotLast()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        int cnt = 5, c = 0;
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                cnt++;
            }
        }|]

        return cnt;
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        int cnt = 5, c = 0;
        cnt += (from int n1 in nums
                from int n2 in nums
                select n1).Count();
        return cnt;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountInAssignmentToZero()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        int cnt;
        cnt = 0;
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                cnt++;
            }
        }|]

        return cnt;
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        int cnt;
        return (from int n1 in nums
                from int n2 in nums
                select n1).Count();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountInAssignmentToNonZero()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        int cnt;
        cnt = 5;
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                cnt++;
            }
        }|]

        return cnt;
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        int cnt;
        cnt = 5;
        cnt += (from int n1 in nums
                from int n2 in nums
                select n1).Count();
        return cnt;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountInParameterAssignedToZeroAndReturned()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums, int c)
    {
        c = 0;
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                c++;
            }
        }|]
        return c;
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums, int c)
    {
        c = (from int n1 in nums
             from int n2 in nums
             select n1).Count();
        return c;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountDeclareWithNonZero()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        var count = 5;
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                count++;
            }
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        var count = 5;
        count += (from int n1 in nums
                  from int n2 in nums
                  select n1).Count();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountAssignWithZero()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        int count = 1;
        count = 0;
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                count++;
            }
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        int count = 1;
        count = (from int n1 in nums
                 from int n2 in nums
                 select n1).Count();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountAssignWithNonZero()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        var count = 0;
        count = 4;
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                count++;
            }
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        var count = 0;
        count = 4;
        count += (from int n1 in nums
                  from int n2 in nums
                  select n1).Count();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountAssignPropertyAssignedToZero()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class A { public int B { get; set; }}
class C
{
    void M(IEnumerable<int> nums, A a)
    {
        a.B = 0;
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                a.B++;
            }
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class A { public int B { get; set; }}
class C
{
    void M(IEnumerable<int> nums, A a)
    {
        a.B = (from int n1 in nums
               from int n2 in nums
               select n1).Count();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountAssignPropertyAssignedToNonZero()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class A { public int B { get; set; }}
class C
{
    void M(IEnumerable<int> nums, A a)
    {
        a.B = 5;
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                a.B++;
            }
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class A { public int B { get; set; }}
class C
{
    void M(IEnumerable<int> nums, A a)
    {
        a.B = 5;
        a.B += (from int n1 in nums
                from int n2 in nums
                select n1).Count();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountAssignPropertyNotKnownAssigned()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class A { public int B { get; set; }}
class C
{
    void M(IEnumerable<int> nums, A a)
    {
        [|foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                a.B++;
            }
        }|]
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class A { public int B { get; set; }}
class C
{
    void M(IEnumerable<int> nums, A a)
    {
        a.B += (from int n1 in nums
                from int n2 in nums
                select n1).Count();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CountIQueryableInInvocation()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class C
{
    void M(IEnumerable<int> nums)
    {
        int c = 0;
        [|foreach (int n1 in nums.AsQueryable())
        {
            c++;
        }|]
    }
}";
            string output = @"
using System.Collections.Generic;
using System.Linq;

class C
{
    void M(IEnumerable<int> nums)
    {
        int c = (from int n1 in nums.AsQueryable()
                 select n1).Count();
    }
}";

            await TestInRegularAndScriptAsync(source, output);
        }

        #endregion

        #region Comments

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CommentsYieldReturn()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        [|// 1
        foreach /* 2 */( /* 3 */ var /* 4 */ x /* 5 */ in /* 6 */ nums /* 7 */)// 8
        {
            // 9
            /* 10 */
            foreach /* 11 */ (/* 12 */ int /* 13 */ y /* 14 */ in /* 15 */ nums /* 16 */)/* 17 */ // 18
            {// 19
             /*20 */
                if /* 21 */(/* 22 */ x > 2 /* 23 */) // 24
                { // 25
                  /* 26 */
                    yield /* 27 */ return /* 28 */ x * y /* 29 */; // 30
                    /* 31 */
                }// 32
                 /* 33 */
            } // 34
              /* 35 */
        }|] /* 36 */
          /* 37 */
        yield  /* 38 */ break/* 39*/; // 40
    }
}";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        return
        // 25
        // 1
        from/* 3 *//* 2 *//* 4 */x /* 5 */ in/* 6 */nums/* 7 */// 8
                                                               // 9
                                                               /* 10 */
        from/* 12 *//* 11 */int /* 13 */ y /* 14 */ in/* 15 */nums/* 16 *//* 17 */// 18
                                                                                  // 19
                                                                                  /*20 */
        where/* 21 *//* 22 */x > 2/* 23 */// 24
                                          /* 26 *//* 27 *//* 28 */
        select x * y/* 29 *//* 31 */// 32
                                    /* 33 */// 34
                                            /* 35 *//* 36 */// 30
                                                            /* 37 *//* 38 *//* 39*/// 40
        ;
    }
}";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CommentsToList()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        /* 1 */ var /* 2 */ list /* 3 */ = /* 4 */ new List<int>(); // 5
        /* 6 */ [|foreach /* 7 */ (/* 8 */ var /* 9 */ x /* 10 */ in /* 11 */ nums /* 12 */) // 13
              /* 14 */{ // 15
            /* 16 */var /* 17 */ y /* 18 */ = /* 19 */ x + 1 /* 20 */; //21
            /* 22 */ list.Add(/* 23 */y /* 24 */) /* 25 */;//26
        /*27*/} //28|]
        /*29*/return /*30*/ list /*31*/; //32
    }
}";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        /*29*/
        return /*30*/ /* 1 *//* 2 *//* 3 *//* 4 */// 5
                                                  /*31*/
(
/* 6 */from/* 8 *//* 7 *//* 9 */x /* 10 */ in/* 11 */nums/* 12 */// 13
                                                                        /* 14 */// 15
                                                                                /* 16 *//* 17 */
              let y /* 18 */ = /* 19 */ x + 1/* 20 *///21
              select y/* 24 *//*27*///28
).ToList()/* 22 *//* 23 *//* 25 *///26
; //32
    }
}";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CommentsCount()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        /* 1 */ var /* 2 */ c /* 3 */ = /* 4 */ 0; // 5
        /* 6 */ [| foreach /* 7 */ (/* 8 */ var /* 9 */ x /* 10 */ in /* 11 */ nums /* 12 */) // 13
        /* 14 */{ // 15
            /* 16 */ c++ /* 17 */;//18
        /*19*/}|] //20
        /*21*/return /*22*/ c /*23*/; //24
    }
}";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        /*21*/
        return /*22*/ /* 1 *//* 2 *//* 3 *//* 4 */// 5
                                                  /*23*/
(
          /* 14 */// 15
                  /* 6 */from/* 8 *//* 7 *//* 9 */x /* 10 */ in/* 11 */nums/* 12 */// 13
       select x/* 10 *//*19*///20
).Count()/* 16 *//* 17 *///18
; //24
    }
}";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task CommentsDefault()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        [|/* 1 */ foreach /* 2 */(int /* 3 */ n1 /* 4 */in /* 5 */ nums /* 6 */)// 7
        /* 8*/{// 9
            /* 10 */int /* 11 */ a /* 12 */ = /* 13 */ n1 + n1 /* 14*/, /* 15 */ b /*16*/ = /*17*/ n1 * n1/*18*/;//19
            /*20*/Console.WriteLine(a + b);//21
        /*22*/}/*23*/|]
    }
}";

            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        foreach (var (a /* 12 */ , b /*16*/ ) in
/* 1 */from/* 2 */int /* 3 */ n1 /* 4 */in/* 5 */nums/* 6 */// 7
                                                                                                            /* 8*/// 9
                                                                                                                  /* 10 *//* 11 */
                                                       let a /* 12 */ = /* 13 */ n1 + n1/* 14*//* 15 */
                                                       let b /*16*/ = /*17*/ n1 * n1/*18*///19
                                                       select (a /* 12 */ , b /*16*/ )/*22*//*23*/)
        {
            /*20*/
            Console.WriteLine(a + b);//21
        }
    }
}";

            await TestInRegularAndScriptAsync(source, output);
        }

        #endregion

        #region Preprocessor directives

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
        public async Task NoConversionPreprocessorDirectives()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        [|foreach(var x in nums)
        {
#if (true)
            yield return x + 1;
#endif
        }|]
    }
}";

            // Cannot convert expressions with preprocessor directives
            await TestMissingAsync(source);
        }

        #endregion
    }
}
