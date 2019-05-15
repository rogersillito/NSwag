﻿//-----------------------------------------------------------------------
// <copyright file="SwaggerToCSharpGeneratorBase.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Linq;
using NJsonSchema;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag.CodeGeneration.CSharp.Models;

namespace NSwag.CodeGeneration.CSharp
{
    /// <summary>The CSharp generator base class.</summary>
    public abstract class SwaggerToCSharpGeneratorBase : ClientGeneratorBase<CSharpOperationModel, CSharpParameterModel, CSharpResponseModel>
    {
        private readonly SwaggerToCSharpGeneratorSettings _settings;
        private readonly CSharpTypeResolver _resolver;
        private readonly SwaggerDocument _document;

        /// <summary>Initializes a new instance of the <see cref="SwaggerToCSharpGeneratorBase"/> class.</summary>
        /// <param name="document">The document.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="resolver">The resolver.</param>
        protected SwaggerToCSharpGeneratorBase(SwaggerDocument document, SwaggerToCSharpGeneratorSettings settings, CSharpTypeResolver resolver)
            : base(document, settings.CodeGeneratorSettings, resolver)
        {
            _document = document;
            _settings = settings;
            _resolver = resolver;
        }

        /// <summary>Gets the type.</summary>
        /// <param name="schema">The schema.</param>
        /// <param name="isNullable">if set to <c>true</c> [is nullable].</param>
        /// <param name="typeNameHint">The type name hint.</param>
        /// <returns>The type name.</returns>
        public override string GetTypeName(JsonSchema4 schema, bool isNullable, string typeNameHint)
        {
            if (schema == null)
                return "void";

            if (schema.ActualTypeSchema.IsBinary)
            {
                return GetBinaryResponseTypeName();
            }

            if (schema.ActualSchema.IsAnyType)
            {
                return "object";
            }

            return Resolver.Resolve(schema.ActualSchema, isNullable, typeNameHint)
                .Replace(_settings.CSharpGeneratorSettings.ArrayType + "<", _settings.ResponseArrayType + "<")
                .Replace(_settings.CSharpGeneratorSettings.DictionaryType + "<", _settings.ResponseDictionaryType + "<");
        }

        /// <summary>Creates a new resolver, adds the given schema definitions and registers an exception schema if available.</summary>
        /// <param name="settings">The settings.</param>
        /// <param name="document">The document </param>
        public static CSharpTypeResolver CreateResolverWithExceptionSchema(CSharpGeneratorSettings settings, SwaggerDocument document)
        {
            var exceptionSchema = document.Definitions.ContainsKey("Exception") ? document.Definitions["Exception"] : null;

            var resolver = new CSharpTypeResolver(settings, exceptionSchema);
            resolver.RegisterSchemaDefinitions(document.Definitions
                .Where(p => p.Value != exceptionSchema)
                .ToDictionary(p => p.Key, p => p.Value));

            return resolver;
        }

        /// <summary>Generates the file.</summary>
        /// <param name="clientTypes">The client types.</param>
        /// <param name="dtoTypes">The DTO types.</param>
        /// <param name="outputType">Type of the output.</param>
        /// <returns>The code.</returns>
        protected override string GenerateFile(CodeArtifactCollection clientTypes, CodeArtifactCollection dtoTypes, ClientGeneratorOutputType outputType)
        {
            var model = new CSharpFileTemplateModel(clientTypes, dtoTypes, outputType, _document, _settings, this, (CSharpTypeResolver)Resolver);
            var template = _settings.CodeGeneratorSettings.TemplateFactory.CreateTemplate("CSharp", "File", model);
            return template.Render();
        }

        /// <summary>Generates all DTO types.</summary>
        /// <returns>The code artifact collection.</returns>
        protected override CodeArtifactCollection GenerateDtoTypes()
        {
            var generator = new CSharpGenerator(_document, _settings.CSharpGeneratorSettings, _resolver);
            return generator.GenerateTypes();
        }
    }
}