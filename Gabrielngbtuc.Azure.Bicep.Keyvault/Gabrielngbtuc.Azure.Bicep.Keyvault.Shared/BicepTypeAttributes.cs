using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.Shared;

/// <summary>
/// When used on a property. Means that the property will be exposed as a property of the bicep type.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
[ExcludeFromCodeCoverage]
public class TypeAnnotationAttribute : Attribute
{
    /// <summary>
    /// Defines a property that will be added to the bicep type.
    /// </summary>
    /// <param name="description"></param>
    /// <param name="flags"></param>
    /// <param name="isSecure"></param>
    public TypeAnnotationAttribute(
        string? description,
        ObjectTypePropertyFlags flags = ObjectTypePropertyFlags.None,
        bool isSecure = false)
    {
        Description = description;
        Flags = flags;
        IsSecure = isSecure;
    }

    /// <summary>
    /// The description of the property
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Flags to be assigned to the property
    /// </summary>
    public ObjectTypePropertyFlags Flags { get; }

    /// <summary>
    /// If the property should be considered as a secret
    /// </summary>
    public bool IsSecure { get; }
}

/// <summary>
/// When used on a class. Means that it will be generated as a child resource of the parent type provided.
/// Example: If the class is "Issue" and the parent is "Repository". The type will be generated with the name "Repository/Issue"
/// </summary>
/// <example>
///<code>
/// 
///    [BicepSerializableType]
/// /* Serialized type will be Repository */
///   public record Repository(
///    [property: TypeAnnotation("The name of the repository", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
///    string Name);
///[BicepSerializableType]
///[BicepParentType(typeof(Repository))]
/// /* Serialized type will be Repository/Label */
/// public record Label([Property: TypeAnnotation("The name of the label", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)] string Name);
/// [BicepSerializableType]
/// [BicepParentType(typeof(Repository))]
/// /* Serialized type will be Repository/Issue */
/// public record Issue(
///    [property: TypeAnnotation("The title of the issue", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
///    string Title,
///    [property: TypeAnnotation("The body of the issue", ObjectTypePropertyFlags.Required)]
///    string Body
/// );
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class)]
[ExcludeFromCodeCoverage]
public class BicepParentTypeAttribute : Attribute
{
    public BicepParentTypeAttribute(
        Type parentType)
    {
        ParentType = parentType;
    }

    /// <summary>
    /// The bicep serializable type that is the parent of this type
    /// </summary>
    public Type ParentType { get; }
}

/// <summary>
/// <para>When used on a class. Means that it will be generated as a bicep resource with the same name as the class. </para>
/// <para> Is not required for types that are used as properties or for the type used to configure an extension. </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
[ExcludeFromCodeCoverage]
public class BicepSerializableType : Attribute;

/// <summary>
/// <para>When used, generates a tagged union type that uses all the types provided and the provided property name as discriminator</para>
/// <para>When creating a bicep type with this attribute it is recommended to:</para>
/// <para>1. Use inheritance and records on the referenced types</para>
/// <para>2. Define the property referenced by <see cref="DiscriminatorPropertyName"/> in the inherited types</para>
/// <para>3. Use enums and the attribute <see cref="BicepStringLiteralValue"/> along the <see langword="nameOf"/> keyword to avoid magic values</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
[ExcludeFromCodeCoverage]
public class BicepDiscriminatorType : Attribute
{
    /// <summary>
    /// The name of the bicep type that will be generated. Recommended to use nameof(T).
    /// </summary>
    public string DiscrminatorTypeName { get; }

    /// <summary>
    /// The list of types that will be part of the union. All of these types should include a property with the name specified by DiscriminatorPropertyName and implementing the attribute TypeAnnotationAttribute
    /// </summary>
    public Dictionary<string, Type> DiscriminatorTypes { get; }

    /// <summary>
    /// The name of the property shared by the discriminated types. Should use pascalCase.
    /// </summary>
    public string DiscriminatorPropertyName { get; }

    public BicepDiscriminatorType(string discrminatorTypeName, string discriminatorPropertyName,
        params Type[] discriminatorTypes)
    {
        DiscrminatorTypeName = discrminatorTypeName;
        DiscriminatorTypes = discriminatorTypes.ToDictionary(t => t.Name, t => t);
        DiscriminatorPropertyName = discriminatorPropertyName;
    }
}

/// <summary>
/// Tells to the type generator that the following property is a string literal with a hardcoded value. To be used on the property that
/// allows to differentiate discriminated types
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
[ExcludeFromCodeCoverage]
public class BicepStringLiteralValue : Attribute
{
    public string Value { get; }

    public BicepStringLiteralValue(string value)
    {
        Value = value;
    }
}

[AttributeUsage(AttributeTargets.Property)]
[ExcludeFromCodeCoverage]
public class BicepNullableType : Attribute;

[AttributeUsage(AttributeTargets.Property)]
[ExcludeFromCodeCoverage]
public class BicepNonNullableString : Attribute;

[AttributeUsage(AttributeTargets.Property)]
[ExcludeFromCodeCoverage]
public class BicepStringLiteralUnion : Attribute
{
    public string[] Values { get; }
    public bool AcceptsNull { get; }
    public BicepStringLiteralUnion(bool acceptsNull = false, params string[] values)
    {
        Values = values;
    }
}

/// <summary>
/// Sets the sensitivity of a bicep string. Is inherited if set on a property with type string[]
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
[ExcludeFromCodeCoverage]
public class BicepStringTypeSensitiveAttribute : Attribute
{
    public bool Sensitive { get; }

    public BicepStringTypeSensitiveAttribute(bool sensitive = true)
    {
        Sensitive = sensitive;
    }
}

/// <summary>
/// Specifies the minimum length constraint for a property when exposed as part of a Bicep type.
/// Is inherited if set on a property with type string[]
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
[ExcludeFromCodeCoverage]
public class BicepStringTypeMinimumLengthAttribute : Attribute
{
    public long Length { get; }

    public BicepStringTypeMinimumLengthAttribute(long length)
    {
        Length = length;
    }
}

/// <summary>
/// Specifies a maximum character length restriction for a property when working with Bicep type definitions.
/// This attribute is used to annotate properties with a maximum allowable string length.
/// Is inherited if set on a property with type string[]
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
[ExcludeFromCodeCoverage]
public class BicepStringTypeMaximumLengthAttribute : Attribute
{
    public long Length { get; }

    public BicepStringTypeMaximumLengthAttribute(long length)
    {
        Length = length;
    }
}

/// <summary>
/// Specifies a regular expression pattern that a bicep string must match.
/// Is inherited if set on a property with type string[]
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
[ExcludeFromCodeCoverage]
public class BicepStringTypeRegexPatternAttribute : Attribute
{
    public string Pattern { get; }

    public BicepStringTypeRegexPatternAttribute(string pattern)
    {
        Pattern = pattern;
    }
}

[ExcludeFromCodeCoverage]
public class BicepAllowInheritedPropertiesAttribute : Attribute;

/// <summary>
/// JSON converter to be used with nullable enum (Enum?) inside Bicep serializable types
/// </summary>
/// <typeparam name="T">The type of the enum to deserialize</typeparam>
[ExcludeFromCodeCoverage]
public class NullableBicepEnumConverter<T> : JsonConverter<T?> where T : struct, Enum
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null; // Renvoie null si la valeur est null dans le JSON
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Unexpected token parsing enum. Expected String, got {reader.TokenType}.");
        }

        var enumText = reader.GetString();

        if (Enum.TryParse(enumText, ignoreCase: true, out T value))
        {
            return value; // Retourne l'énumération correspondante
        }

        throw new JsonException($"Unable to convert \"{enumText}\" to Enum \"{typeof(T)}\".");
    }

    public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue(); // Sérialise les valeurs nulles en JSON comme null
        }
        else
        {
            writer.WriteStringValue(value.Value.ToString()); // Sérialise le nom littéral de l'énum
        }
    }
}

/// <summary>
/// JSON converter to be used with enum inside Bicep serializable types
/// </summary>
/// <typeparam name="T">The type of the enum to deserialize</typeparam>
[ExcludeFromCodeCoverage]
public class BicepEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Unexpected token parsing enum. Expected String, got {reader.TokenType}.");
        }

        var enumText = reader.GetString();

        if (Enum.TryParse(enumText, ignoreCase: true, out T value))
        {
            return value; // Retourne l'énumération correspondante
        }
        
        if(EnumHelper.GetEnumFromDisplayName<T>(enumText) is T enumFromDisplayName)
            return enumFromDisplayName;

        throw new JsonException($"Unable to convert \"{enumText}\" to Enum \"{typeof(T)}\".");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString()); // Sérialise le nom littéral de l'énum
    }
}

/// <summary>
/// JSON converter to deserialize arrays of nullable enums (Enum?) inside Bicep serializable types
/// </summary>
/// <typeparam name="T">The type of the enum to deserialize</typeparam>
[ExcludeFromCodeCoverage]
public class NullableBicepEnumArrayConverter<T> : JsonConverter<T?[]> where T : struct, Enum
{
    public override T?[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null; // Renvoie null si la valeur est null dans le JSON
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Unexpected token parsing array. Expected StartArray, got {reader.TokenType}.");
        }

        var values = new List<T?>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break; // Fin du tableau
            }

            if (reader.TokenType == JsonTokenType.Null)
            {
                values.Add(null); // Ajoute une valeur nulle dans le tableau
                continue;
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Unexpected token parsing enum. Expected String, got {reader.TokenType}.");
            }

            var enumText = reader.GetString();

            if (Enum.TryParse(enumText, ignoreCase: true, out T value))
            {
                values.Add(value);
            }
            else
            {
                throw new JsonException($"Unable to convert \"{enumText}\" to Enum \"{typeof(T)}\".");
            }
        }

        return values.ToArray();
    }

    public override void Write(Utf8JsonWriter writer, T?[] value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue(); // Sérialise une valeur null
            return;
        }

        writer.WriteStartArray();

        foreach (var item in value)
        {
            if (item is null)
            {
                writer.WriteNullValue(); // Gère les valeurs nulles
            }
            else
            {
                writer.WriteStringValue(item.Value.ToString());
            }
        }

        writer.WriteEndArray();
    }
}

/// <summary>
/// JSON converter to deserialize arrays of enums inside Bicep serializable types
/// </summary>
/// <typeparam name="T">The type of the enum to deserialize</typeparam>
[ExcludeFromCodeCoverage]
public class BicepEnumArrayConverter<T> : JsonConverter<T[]> where T : struct, Enum
{
    public override T[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null; // Renvoie null si la valeur est null dans le JSON
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Unexpected token parsing array. Expected StartArray, got {reader.TokenType}.");
        }

        var values = new List<T>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break; // Fin du tableau
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Unexpected token parsing enum. Expected String, got {reader.TokenType}.");
            }

            var enumText = reader.GetString();

            if (Enum.TryParse(enumText, ignoreCase: true, out T value))
            {
                values.Add(value);
            }
            else
            {
                throw new JsonException($"Unable to convert \"{enumText}\" to Enum \"{typeof(T)}\".");
            }
        }

        return values.ToArray();
    }

    public override void Write(Utf8JsonWriter writer, T[] value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue(); // Sérialise une valeur null
            return;
        }

        writer.WriteStartArray();

        foreach (var item in value)
        {
            writer.WriteStringValue(item.ToString());
        }

        writer.WriteEndArray();
    }
}

[ExcludeFromCodeCoverage]
public class StringToIntJsonConverter : JsonConverter<int>
{
    // Désérialisation : Convertir une chaîne en un entier
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Vérifier si la valeur dans JSON est une chaîne valide
        if (reader.TokenType == JsonTokenType.String)
        {
            string stringValue = reader.GetString();

            // Tenter de convertir la chaîne en un entier
            if (int.TryParse(stringValue, out int intValue))
            {
                return intValue;
            }
            else
            {
                throw new JsonException($"Impossible de convertir '{stringValue}' en entier.");
            }
        }

        // Si la valeur dans JSON n'est pas une chaîne, lancer une exception
        throw new JsonException($"Le type de token JSON est invalide : {reader.TokenType}");
    }

    // Sérialisation : Convertir un entier en une chaîne
    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        // Convertir l'entier en une chaîne et l'écrire
        writer.WriteStringValue(value.ToString());
    }
}
