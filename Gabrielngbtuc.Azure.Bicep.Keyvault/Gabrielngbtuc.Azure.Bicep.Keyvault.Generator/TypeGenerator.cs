using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using Azure.Bicep.Types;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;
using Azure.Bicep.Types.Serialization;
using Gabrielngbtuc.Azure.Bicep.Keyvault.Generator;
using Gabrielngbtuc.Azure.Bicep.Keyvault.Shared;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.Generator;

public static class TypeFactoryExtensions
{
    public static ITypeReference GetReferenceFromType(this TypeFactory factory, TypeBase type)
    {
        try
        {
            var typeReference = factory.Create(() => type);
            return factory.GetReference(typeReference);
        }
        catch (ArgumentException ex)
        {
        }

        return factory.GetReference(type);
    }
}

public static class TypeGenerator
{
    internal static string CamelCase(string input)
        => $"{input[..1].ToLowerInvariant()}{input[1..]}";

    internal static TypeBase GenerateForRecord(TypeFactory factory, ConcurrentDictionary<Type, TypeBase> typeCache,
        Type type, bool ignoreDiscriminatorAttribute = false)
    {
        TypeBase GenerateProperties(PropertyInfo property, ref Dictionary<string, ObjectTypeProperty> typeProperties)
        {
            var annotation = property.GetCustomAttributes<TypeAnnotationAttribute>(false).FirstOrDefault();
            var propertyType = property.PropertyType;
            TypeBase typeReference;
            var originalPropertyType = propertyType;

            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                propertyType = propertyType.GetGenericArguments()[0];
            }

            if (property.GetCustomAttribute<BicepStringLiteralValue>(true) is { } discriminatorTypeValue)
            {
                typeReference = factory.GetReferenceFromType(new StringLiteralType(discriminatorTypeValue.Value)).Type;
            }
            //Handle union type attributes
            //Integer union type
            else if (property.GetCustomAttribute<BicepStringLiteralUnion>() is { } stringLiteralUnionTypeAttribute)
            {
                var unionTypeReferences = stringLiteralUnionTypeAttribute.Values
                    .Select(x => factory.GetReferenceFromType(new StringLiteralType(x)))
                    .ToList();
                if (stringLiteralUnionTypeAttribute.AcceptsNull)
                {
                    unionTypeReferences.Add(factory.GetReferenceFromType(new NullType()));
                }
                typeReference = factory.GetReferenceFromType(new UnionType(
                    unionTypeReferences
                )).Type;
            }
            else if (propertyType == typeof(string) && annotation?.IsSecure == true)
            {
                var maximumLengthAttribute = property.GetCustomAttribute<BicepStringTypeMaximumLengthAttribute>(true);
                var minimumLengthAttribute = property.GetCustomAttribute<BicepStringTypeMinimumLengthAttribute>(true);
                var regexPatternAttribute = property.GetCustomAttribute<BicepStringTypeRegexPatternAttribute>(true);
                typeReference = factory.GetReferenceFromType(new StringType(sensitive: true,
                    minimumLengthAttribute?.Length,
                    maximumLengthAttribute?.Length,
                    regexPatternAttribute?.Pattern)).Type;
            }
            else if (propertyType == typeof(string) || propertyType == typeof(DateTimeOffset))
            {
                var maximumLengthAttribute = property.GetCustomAttribute<BicepStringTypeMaximumLengthAttribute>(true);
                var minimumLengthAttribute = property.GetCustomAttribute<BicepStringTypeMinimumLengthAttribute>(true);
                var regexPatternAttribute = property.GetCustomAttribute<BicepStringTypeRegexPatternAttribute>(true);
                typeReference =
                    factory.GetReferenceFromType(new StringType(
                        annotation?.IsSecure,
                        minimumLengthAttribute?.Length,
                        maximumLengthAttribute?.Length,
                        regexPatternAttribute?.Pattern
                        )).Type;
            }
            else if (propertyType == typeof(bool))
            {
                typeReference = typeCache.GetOrAdd(propertyType,
                    _ => factory.GetReferenceFromType(new BooleanType()).Type);
            }
            else if (propertyType == typeof(int))
            {
                typeReference = typeCache.GetOrAdd(propertyType,
                    _ => factory.GetReferenceFromType(new IntegerType()).Type);
            }
            else if (propertyType.IsClass)
            {
                typeReference = typeCache.GetOrAdd(propertyType,
                    _ => factory.GetReferenceFromType(GenerateForRecord(factory, typeCache, propertyType)).Type);
            }
            else if (originalPropertyType.IsGenericType &&
                     originalPropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                     originalPropertyType.GetGenericArguments()[0] is { IsEnum: true } enumType)
            {
                var enumMembers = enumType.GetEnumNames()
                    .Select(x => factory.GetReferenceFromType(new StringLiteralType(x)).Type)
                    .Select(factory.GetReference)
                    .ToImmutableArray();

                typeReference = typeCache.GetOrAdd(propertyType,
                    _ => factory.GetReferenceFromType(new UnionType(enumMembers)).Type);
            }
            else if (originalPropertyType is { IsEnum: true } originalEnumType)
            {
                var enumMembers = originalEnumType.GetEnumNames()
                    .Select(x => factory.GetReferenceFromType(new StringLiteralType(x)).Type)
                    .Select(factory.GetReference)
                    .ToImmutableArray();

                typeReference = typeCache.GetOrAdd(propertyType,
                    _ => factory.GetReferenceFromType(new UnionType(enumMembers)).Type);
            }
            else
            {
                throw new NotImplementedException($"Unsupported property type {propertyType}");
            }

            if (property.GetCustomAttribute<BicepNonNullableString>() is null &&  (property.GetCustomAttribute<BicepNullableType>() is not null || IsNullableReferenceType(property)
                || (originalPropertyType.IsGenericType &&
                    originalPropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))))
            {
                var unionType = factory.GetReferenceFromType(new UnionType([
                    factory.GetReference(typeReference),
                    factory.GetReferenceFromType(new NullType())
                ]));
                typeReference = unionType.Type;
            }

            typeProperties[CamelCase(property.Name)] = new ObjectTypeProperty(
                factory.GetReference(typeReference),
                annotation?.Flags ?? ObjectTypePropertyFlags.None,
                annotation?.Description);

            return typeReference;
        }

        var typeProperties = new Dictionary<string, ObjectTypeProperty>();
        //Updated discriminator handling
        if (ignoreDiscriminatorAttribute == false &&
            type.GetCustomAttribute<JsonPolymorphicAttribute>() is { } polymorphicAttribute
            && type.GetCustomAttributes<JsonDerivedTypeAttribute>() is { } derivedTypesAttribute
            && type.GetCustomAttribute<BicepDiscriminatorType>() is
                { } discriminatorTypeAttribute)
        {
            var baseProperties = (ObjectType)GenerateForRecord(factory, typeCache, type, true);
            var childTypesDictionary = new Dictionary<string, ITypeReference>();
            var unionType = typeCache.GetOrAdd(discriminatorTypeAttribute.DiscriminatorType, _ => factory.GetReferenceFromType(new UnionType(derivedTypesAttribute.Select(
                dt => factory.GetReferenceFromType(new StringLiteralType(dt.TypeDiscriminator.ToString()))
                ).ToList()
            )).Type);
            foreach (var derivedType in derivedTypesAttribute)
            {
                var discriminatedTypeProperties = typeCache.GetOrAdd(derivedType.DerivedType, _ => (ObjectType)GenerateForRecord(factory, typeCache, derivedType.DerivedType, true));
                var t = (ObjectType)discriminatedTypeProperties;
                var newProps =
                    new Dictionary<string, ObjectTypeProperty>()
                    {
                        {
                            polymorphicAttribute.TypeDiscriminatorPropertyName!,
                            new ObjectTypeProperty(factory.GetReferenceFromType(new StringLiteralType(derivedType.TypeDiscriminator.ToString())), ObjectTypePropertyFlags.Required, "The discriminator for derived types.")
                        }
                    }
                ;
                foreach (var kvp in t.Properties)
                {
                    newProps.Add(kvp.Key, kvp.Value);
                }
                var newObjectType = new ObjectType(t.Name, 
                    newProps.Union(t.Properties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
                        .ToImmutableDictionary()
                    , t.AdditionalProperties);
                childTypesDictionary.Add(derivedType.DerivedType.Name, factory.GetReferenceFromType(newObjectType));
            }

            var typeReference = typeCache.GetOrAdd(type, _ => factory.GetReferenceFromType(new DiscriminatedObjectType(
                type.Name,
                polymorphicAttribute.TypeDiscriminatorPropertyName!, baseProperties.Properties
                , childTypesDictionary)).Type);

            return typeReference;
        }
        //Handle discriminator
        // if (ignoreDiscriminatorAttribute == false && type.GetCustomAttribute<BicepDiscriminatorType>() is
        //         { } discriminatorTypeAttribute)
        // {
        //     var baseProperties = (ObjectType)GenerateForRecord(factory, typeCache, type, true);
        //     var childTypesDictionary = new Dictionary<string, ITypeReference>();
        //     foreach (var kvp in discriminatorTypeAttribute.DiscriminatorTypes)
        //     {
        //         var discriminatedTypeProperties = typeCache.GetOrAdd(kvp.Value, _ => (ObjectType)GenerateForRecord(factory, typeCache, kvp.Value, true));
        //         childTypesDictionary.Add(kvp.Key, factory.GetReferenceFromType(discriminatedTypeProperties));
        //     }
        //
        //     var typeReference = typeCache.GetOrAdd(type, _ => factory.GetReferenceFromType(new DiscriminatedObjectType(
        //         discriminatorTypeAttribute.DiscrminatorTypeName,
        //         discriminatorTypeAttribute.DiscriminatorPropertyName, baseProperties.Properties
        //         , childTypesDictionary)).Type);
        //
        //     return typeReference;
        // }
        //Handle dictionaries
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var genericArguments = type.GetGenericArguments();
            if (genericArguments.Length != 2)
            {
                throw new ArgumentException("Dictionary must have exactly two generic arguments");
            }

            if (genericArguments[0] != typeof(string))
            {
                throw new ArgumentException("Dictionary must have string as key");
            }

            var valueType = genericArguments[1];
            ITypeReference additionalPropertiesReference;
            if (!valueType.IsPrimitive && valueType != typeof(string))
                additionalPropertiesReference =
                    factory.GetReferenceFromType(typeCache.GetOrAdd(valueType, _ => GenerateForRecord(factory, typeCache, valueType)));
            else
            {
                var sensitiveAttribute = valueType.GetCustomAttribute<BicepStringTypeSensitiveAttribute>(true);
                var maximumLengthAttribute = valueType.GetCustomAttribute<BicepStringTypeMaximumLengthAttribute>(true);
                var minimumLengthAttribute = valueType.GetCustomAttribute<BicepStringTypeMinimumLengthAttribute>(true);
                var regexPatternAttribute = valueType.GetCustomAttribute<BicepStringTypeRegexPatternAttribute>(true);
                additionalPropertiesReference = factory.GetReferenceFromType(
                    new StringType(
                        sensitiveAttribute?.Sensitive,
                        minimumLengthAttribute?.Length,
                        maximumLengthAttribute?.Length,
                        regexPatternAttribute?.Pattern
                        ));
            }

            TypeBase typeReference = factory.GetReferenceFromType(typeCache.GetOrAdd(type, _ =>  new ObjectType(type.ToString(),
                new Dictionary<string, ObjectTypeProperty>(),
                additionalPropertiesReference))).Type;
            typeCache.GetOrAdd(type, _ => typeReference);
            return typeReference;
        }
        //Handle arrays
        else if (type.IsArray)
        {
            var sensitiveAttribute = type.GetCustomAttribute<BicepStringTypeSensitiveAttribute>(true);
            var maximumLengthAttribute = type.GetCustomAttribute<BicepStringTypeMaximumLengthAttribute>(true);
            var minimumLengthAttribute = type.GetCustomAttribute<BicepStringTypeMinimumLengthAttribute>(true);
            var regexPatternAttribute = type.GetCustomAttribute<BicepStringTypeRegexPatternAttribute>(true);
            
            var elementType = type.GetElementType();
            var arrayType = elementType == typeof(string)
                ? factory.GetReferenceFromType(new StringType(
                    sensitiveAttribute?.Sensitive,
                    minimumLengthAttribute?.Length,
                    maximumLengthAttribute?.Length,
                    regexPatternAttribute?.Pattern
                    ))
                : factory.GetReferenceFromType(typeCache.GetOrAdd(elementType, _ =>  GenerateForRecord(factory, typeCache, elementType)));
            typeCache.GetOrAdd(type, _ => factory.GetReferenceFromType(
                typeCache.GetOrAdd(type, _ =>  new ArrayType(
                    arrayType
                ))
            ).Type);
        }
        //Handle enum
        else if (type.IsEnum)
        {
            var enumMembers = type.GetEnumNames()
                .Select(x => factory.GetReferenceFromType(typeCache.GetOrAdd(type, _ => new StringLiteralType(x))).Type)
                .Select(factory.GetReference)
                .ToImmutableArray();
            TypeBase typeReference = factory.GetReferenceFromType(typeCache.GetOrAdd(type, _ => new UnionType(enumMembers))).Type;
            typeCache.GetOrAdd(type, _ => typeReference);
            return typeReference;
        }
        else
        {
            var properties = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (type.GetCustomAttribute<BicepAllowInheritedPropertiesAttribute>() is null)
            {
                properties = properties.Where(p =>
                    !type.BaseType.GetProperties().Select(prop => prop.Name).Contains(p.Name)).ToArray();
            }
            //Remove properties redefined in the inheriting type
            var redefinedProperties = properties.Where(p => properties.Count(prop => prop.Name == p.Name) > 1
                                                            && p.GetCustomAttribute<TypeAnnotationAttribute>() is null);
            properties = properties.Except(redefinedProperties).ToArray();
            foreach (var property in properties)
            {
                GenerateProperties(property, ref typeProperties);
            }
        }


        return new ObjectType(
            type.Name,
            typeProperties,
            null);
    }

    internal static ResourceType GenerateResource(TypeFactory factory, ConcurrentDictionary<Type, TypeBase> typeCache,
        Type type)
    {
        var realName = type.Name;
        var parentType = type;
        do
        {
            parentType = parentType.GetCustomAttribute<BicepParentTypeAttribute>(true)?.ParentType;
            if (parentType is not null)
            {
                realName = $"{parentType!.Name}/{realName}";
            }
        } while (type.GetCustomAttribute<BicepParentTypeAttribute>(true)?.ParentType is not null &&
                 parentType != null);
        
        
        ResourceTypeFunction resourceTypeFunction = new ResourceTypeFunction(
                factory.GetReferenceFromType(
                    new FunctionType(
                        new []
                        {
                            new FunctionParameter("name", factory.GetReferenceFromType(new StringType()), "The name of the certificate")
                        }
                        , factory.GetReferenceFromType(new ObjectType("Certificate", new Dictionary<string, ObjectTypeProperty>()
                        {
                            {
                                "data",
                                new ObjectTypeProperty(
                                    factory.GetReferenceFromType(new StringType()),
                                    ObjectTypePropertyFlags.None,
                                    "The certificate data"
                                    )
                            }
                        }, null))))
            , "desc");

        return (ResourceType)factory.GetReferenceFromType(new ResourceType(
            realName,
            ScopeType.Unknown,
            null,
            factory.GetReferenceFromType(GenerateForRecord(factory, typeCache, type)),
            ResourceFlags.None,
            realName == "Certificate" ? new Dictionary<string, ResourceTypeFunction>()
            {
                {
                    "listKeys",
                    resourceTypeFunction
                }
            } : null
            )).Type;
    }

    internal static string GetString(Action<Stream> streamWriteFunc)
    {
        using var memoryStream = new MemoryStream();
        streamWriteFunc(memoryStream);

        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    /// <summary>
    /// Generates bicep types and index for the provided assembly
    /// </summary>
    /// <param name="extensionName">Name of the extension</param>
    /// <param name="version">Version of the extension in SemVersion format</param>
    /// <param name="configurationType">Type used to generate the configuration. Pass <see langword="null"/> if no configuration is required.</param>
    /// <param name="sourceAssemblyType">A type present in the assembly where your serializable types are contained.</param>
    /// <returns></returns>
    [RequiresUnreferencedCode("Retrieves the valid bicep types from the current assembly")]
    public static Dictionary<string, string> GenerateTypes(string extensionName, string version,
        Type? configurationType, Type? sourceAssemblyType = null)
    {
        var factory = new TypeFactory([]);

        var typeCache = new ConcurrentDictionary<Type, TypeBase>();


        TypeBase? configurationTypeReference;

        if (configurationType is null)
        {
            configurationTypeReference = null;
        }

        else
        {
            var configurationTypeBase = GenerateForRecord(factory, typeCache, configurationType);

            if (configurationTypeBase is ObjectType configurationTypeBaseObject)
            {
                var configuration =
                    factory.GetReferenceFromType(new ObjectType("configuration", configurationTypeBaseObject.Properties,
                        null)).Type;
                configurationTypeReference = configuration;
            }
            else
            {
                configurationTypeReference = configurationTypeBase;
            }
        }

        var settings = new TypeSettings(
            name: extensionName,
            version: version,
            isSingleton: false,
            configurationType: new CrossFileTypeReference("types.json", factory.GetIndex(configurationTypeReference ??
                factory.GetReferenceFromType(new ObjectType("configuration",
                    new Dictionary<string, ObjectTypeProperty>(), null)).Type
            )));

        var serializableTypes =
            sourceAssemblyType is null
                ? Assembly.GetExecutingAssembly().GetTypes()
                    .Where(t => t.GetCustomAttribute<BicepSerializableType>() != null)
                : Assembly.GetAssembly(sourceAssemblyType).GetTypes()
                    .Where(t => t.GetCustomAttribute<BicepSerializableType>() != null);
        var resourceTypes = serializableTypes.Select(type => GenerateResource(factory, typeCache, type));

        var index = new TypeIndex(
            resourceTypes.ToDictionary(x => x.Name, x => new CrossFileTypeReference("types.json", factory.GetIndex(x))),
            new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<CrossFileTypeReference>>>(),
            settings,
            null);

        return new Dictionary<string, string>
        {
            ["index.json"] = GetString(stream => TypeSerializer.SerializeIndex(stream, index)),
            ["types.json"] = GetString(stream => TypeSerializer.Serialize(stream, factory.GetTypes())),
        };
    }

    private static bool IsNullableReferenceType(PropertyInfo property)
    {
        // Vérification de l'attribut NullableAttribute pour la propriété
        var nullableAttribute = property.CustomAttributes
            .FirstOrDefault(a => a.AttributeType.Name == "NullableAttribute" || a.AttributeType.Name == "Nullable");

        if (nullableAttribute != null)
        {
            // Les arguments de NullableAttribute indiquent :
            // 1 --> Nullable (ex.: string?)
            // 2 --> Non-nullable (ex.: string)
            // var flag = (byte)nullableAttribute.ConstructorArguments[0].Value;
            // return flag == 1;
            return true;
        }

        return false; // Non-nullable ou nullabilité inconnue
    }
}