using System.ComponentModel.DataAnnotations;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.Shared;

using System.Reflection;

public class EnumHelper
{
    /// <summary>
    /// Retrieve the name property from the DisplayAttribute
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string? GetDisplayName(Enum? value)
    {
        if (value == null)
        {
            return null;
        }
        
        // Obtenir le type d'énumération
        Type enumType = value.GetType();

        // Récupérer le membre correspondant à l'énumération
        MemberInfo? memberInfo = enumType.GetMember(value.ToString()).FirstOrDefault();

        if (memberInfo != null)
        {
            // Vérifier si un DisplayAttribute est défini sur ce membre
            var displayAttribute = memberInfo.GetCustomAttribute<DisplayAttribute>();

            // Retourner la propriété Name ou null si aucun DisplayAttribute n'est présent
            return displayAttribute?.Name;
        }

        return null;
    }
    
    /// <summary>
    /// Retrieve the enum value from its DisplayName.
    /// </summary>
    /// <typeparam name="T">The type of the enum.</typeparam>
    /// <param name="displayName">The display name to find.</param>
    /// <returns>The matching enum value, or null if not found.</returns>
    public static T? GetEnumFromDisplayName<T>(string displayName) where T : Enum
    {
        // Vérifier que T est bien un type d'énumération
        if (!typeof(T).IsEnum)
        {
            throw new ArgumentException("T must be an enumerated type.");
        }

        // Parcourir tous les membres de l'énumération
        foreach (var field in typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public))
        {
            // Vérifier l'attribut DisplayAttribute sur le membre
            var displayAttribute = field.GetCustomAttribute<DisplayAttribute>();
            if (displayAttribute != null && displayAttribute.Name == displayName)
            {
                // Retourner la valeur d'énumération correspondante
                return (T)field.GetValue(null);
            }
        }

        // Aucune correspondance trouvée
        return default;
    }
}