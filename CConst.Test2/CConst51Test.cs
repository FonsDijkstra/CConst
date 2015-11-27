using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FonsDijkstra.CConst;
using Xunit;

namespace CConst.Test2
{
    public class CConst51Test
    {
        [Fact]
        public void Override_of_pure_method_may_not_be_impure()
        {
            var source = @"
                using FonsDijkstra.CConst;

                abstract class A
                {
                    [Const]
                    public abstract bool F();
                }

                class B : A
                {
                    public override bool F() // reports diagnostic CConst51
                    {
                        return false;
                    }
                }
            ";
            var diagnostics = DiagnosticHelper.GetDiagnostics(new ConstPolymorphismAnalyzer(), source);
            Assert.True(diagnostics.Single().Id == ConstPolymorphismAnalyzer.OverrideDiagnosticId);
        }

        [Fact]
        public void Override_of_pure_method_must_be_pure()
        {
            var source = @"
                using FonsDijkstra.CConst;

                abstract class A
                {
                    [Const]
                    public abstract bool F();
                }

                class B : A
                {
                    [Const]
                    public override bool F()
                    {
                        return false;
                    }
                }
            ";

            Assert.Empty(DiagnosticHelper.GetDiagnostics(new ConstPolymorphismAnalyzer(), source));
        }

        [Fact]
        public void Override_of_impure_method_may_be_pure()
        {
            var source = @"
                using FonsDijkstra.CConst;

                abstract class A
                {
                    public abstract bool F();
                }

                class B : A
                {
                    [Const]
                    public override bool F()
                    {
                        return false;
                    }
                }
            ";

            Assert.Empty(DiagnosticHelper.GetDiagnostics(new ConstPolymorphismAnalyzer(), source));
        }

        [Fact]
        public void Override_of_impure_method_may_be_impure()
        {
            var source = @"
                abstract class A
                {
                    public abstract bool F();
                }

                class B : A
                {
                    public override bool F()
                    {
                        return false;
                    }
                }
            ";

            Assert.Empty(DiagnosticHelper.GetDiagnostics(new ConstPolymorphismAnalyzer(), source));
        }
    }
}
