﻿using System;
using Shiny.Generators.Tasks;
using Xunit;
using Xunit.Abstractions;


namespace Shiny.Generators.Tests
{
    public class StaticClassSourceGeneratorTests
    {
        readonly ITestOutputHelper output;
        readonly AssemblyGenerator generator;


        public StaticClassSourceGeneratorTests(ITestOutputHelper output)
        {
            this.output = output;
            this.generator = new AssemblyGenerator();
        }


        [Fact]
        public void CoreClasses()
        {
            this.generator.AddSource("[assembly:Shiny.GenerateStaticClasses(\"CoreClasses\")]");
            this.generator.DoGenerate(
                this.output,
                nameof(CoreClasses),
                new StaticClassSourceGenerator(),
                "CoreClasses.ShinyJobs",
                "CoreClasses.ShinyConnectivity",
                "CoreClasses.ShinyPower",
                "CoreClasses.ShinyFileSystem"
            );
        }
    }
}