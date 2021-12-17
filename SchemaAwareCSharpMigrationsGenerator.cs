using System;
using System.Collections.Generic;
using System.Linq;

using DataContextContainer;

using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Utilities;

namespace Microsoft.EntityFrameworkCore.Migrations.Design
{

    public class SchemaAwareCSharpMigrationsGenerator : CSharpMigrationsGenerator
    {
        private readonly ICSharpHelper _code;
        private readonly ICSharpMigrationOperationGenerator _operationGenerator;
        private readonly ISchemaNameProvider schemaNameProvider;

        public SchemaAwareCSharpMigrationsGenerator(MigrationsCodeGeneratorDependencies dependencies,
                                                    CSharpMigrationsGeneratorDependencies csharpDependencies,
                                                    ISchemaNameProvider schemaNameProvider) : base(dependencies, csharpDependencies)
        {
            _code = csharpDependencies.CSharpHelper;
            _operationGenerator = csharpDependencies.CSharpMigrationOperationGenerator;
            this.schemaNameProvider = schemaNameProvider;

            Console.WriteLine("----- SCHEMA AWARE MIGRATIONS GENERATOR ------");
            Console.WriteLine($"Generating Migrations for {this.schemaNameProvider.SchemaName}");
        }

        public override string GenerateMigration(string migrationNamespace,
                                                 string migrationName,
                                                 IReadOnlyList<MigrationOperation> upOperations,
                                                 IReadOnlyList<MigrationOperation> downOperations)
        {
            var builder = new IndentedStringBuilder();
            var isPublic = this.schemaNameProvider.SchemaName == "public";
            var namespaces = new List<string>
            {
                "System",
                "System.Collections.Generic",
                "Microsoft.EntityFrameworkCore.Migrations"
            };
            namespaces.AddRange(GetNamespaces(upOperations.Concat(downOperations)));
            foreach (var n in namespaces.Distinct())
            {
                builder
                    .Append("using ")
                    .Append(n)
                    .AppendLine(";");
            }
            builder
                .AppendLine()
                .Append("namespace ").AppendLine(_code.Namespace(migrationNamespace))
                .AppendLine("{");
            using (builder.Indent())
            {
                builder
                    .Append("public partial class ").Append(_code.Identifier(migrationName)).Append(" : Migration");

                if (!isPublic)
                {
                    builder.AppendLine(", IHaveSchemaName");
                }
                else
                {
                    builder.AppendLine();
                }
                builder.AppendLine("{");
                using (builder.Indent())
                {
                    if (!isPublic)
                    {
                        builder.AppendLine($"public string SchemaName {{ get; set; }} = \"{this.schemaNameProvider.SchemaName}\";");
                    }

                    builder.AppendLine();
                    builder
                        .AppendLine("protected override void Up(MigrationBuilder migrationBuilder)")
                        .AppendLine("{");
                    using (builder.Indent())
                    {
                        _operationGenerator.Generate("migrationBuilder", upOperations, builder);
                    }
                    builder
                        .AppendLine()
                        .AppendLine("}")
                        .AppendLine()
                        .AppendLine("protected override void Down(MigrationBuilder migrationBuilder)")
                        .AppendLine("{");
                    using (builder.Indent())
                    {
                        _operationGenerator.Generate("migrationBuilder", downOperations, builder);
                    }
                    builder
                        .AppendLine()
                        .AppendLine("}");
                }

                builder.AppendLine("}");
            }
            builder.AppendLine("}");

            return builder.ToString();
        }


    }

}