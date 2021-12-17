
using System;
using System.Reflection;
using System.Reflection.Emit;

using Microsoft.EntityFrameworkCore.Migrations;

namespace DataContextContainer
{

    [Migration("Empty1")]
    public class EmptyMigration : Migration
    {
        private static readonly Type type = typeof(EmptyMigration);
        private static readonly AssemblyName assemblyName = new AssemblyName("EmptyMigrationsAssembly");
        private static readonly AssemblyBuilder builder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        private static readonly ModuleBuilder moduleBuilder = builder.DefineDynamicModule(assemblyName.Name);

        static EmptyMigration()
        {
            Instance = new EmptyMigration();
        }

        public static EmptyMigration Instance { get; }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        public static EmptyMigration GetNewInstance()
        {
            var guid = Guid.NewGuid();
            var tb = moduleBuilder.DefineType(type.Name + "Proxy_" + guid, TypeAttributes.Public, type);

            var attrCtorParams = new Type[] { typeof(string) };
            var attrCtorInfo = typeof(MigrationAttribute).GetConstructor(attrCtorParams);
            var attrBuilder = new CustomAttributeBuilder(attrCtorInfo, new object[] { "Empty1" + guid });
            tb.SetCustomAttribute(attrBuilder);

            var newType = tb.CreateType();
            var instance = (EmptyMigration)Activator.CreateInstance(newType);
            return instance;
        }
    }
}
