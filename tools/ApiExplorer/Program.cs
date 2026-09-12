using System.Reflection;
using System.Text.RegularExpressions;

// Reads Valheim's managed assemblies as metadata only (nothing is executed) so you
// can look up the exact type and member signatures a Harmony patch has to match.

if (args.Length < 1)
{
    Console.Error.WriteLine(
        """
        Usage:
          ApiExplorer types <pattern>            list types whose name matches <pattern>
          ApiExplorer members <Type> [pattern]   list members of <Type>
          ApiExplorer assemblies                 list the assemblies being read

        The assembly directory defaults to <repo>/lib/valheim and can be overridden
        with the VALHEIM_MANAGED_DIR environment variable.
        """);
    return 1;
}

var managedDir = Environment.GetEnvironmentVariable("VALHEIM_MANAGED_DIR");
if (string.IsNullOrWhiteSpace(managedDir))
{
    managedDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "lib", "valheim");
}

managedDir = Path.GetFullPath(managedDir);
if (!Directory.Exists(managedDir))
{
    Console.Error.WriteLine($"Managed assembly directory not found: {managedDir}");
    Console.Error.WriteLine("Run tools/fetch-game-libs.sh first.");
    return 1;
}

var assemblyPaths = Directory.GetFiles(managedDir, "*.dll");
if (assemblyPaths.Length == 0)
{
    Console.Error.WriteLine($"No assemblies in {managedDir}.");
    return 1;
}

using var mlc = new MetadataLoadContext(new PathAssemblyResolver(assemblyPaths), "mscorlib");

var command = args[0].ToLowerInvariant();

switch (command)
{
    case "assemblies":
        foreach (var path in assemblyPaths.OrderBy(p => p))
        {
            Console.WriteLine(Path.GetFileName(path));
        }

        return 0;

    case "types":
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("types requires a pattern.");
            return 1;
        }

        var pattern = new Regex(args[1], RegexOptions.IgnoreCase);
        foreach (var type in EnumerateTypes(mlc, assemblyPaths)
                     .Where(t => pattern.IsMatch(t.Name))
                     .OrderBy(t => t.FullName))
        {
            Console.WriteLine($"{type.FullName}  [{type.Assembly.GetName().Name}]");
        }

        return 0;
    }

    case "members":
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("members requires a type name.");
            return 1;
        }

        var target = EnumerateTypes(mlc, assemblyPaths)
            .FirstOrDefault(t => t.FullName == args[1] || t.Name == args[1]);

        if (target is null)
        {
            Console.Error.WriteLine($"Type not found: {args[1]}");
            return 1;
        }

        var filter = args.Length > 2 ? new Regex(args[2], RegexOptions.IgnoreCase) : null;
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                                   BindingFlags.Instance | BindingFlags.Static |
                                   BindingFlags.DeclaredOnly;

        Console.WriteLine($"// {target.FullName} : {target.BaseType?.Name}  [{target.Assembly.GetName().Name}]");

        foreach (var field in target.GetFields(flags).OrderBy(f => f.Name))
        {
            if (filter is not null && !filter.IsMatch(field.Name))
            {
                continue;
            }

            Console.WriteLine($"{FieldAccess(field)} {(field.IsStatic ? "static " : "")}{Short(field.FieldType)} {field.Name};");
        }

        foreach (var method in target.GetMethods(flags).OrderBy(m => m.Name))
        {
            if (filter is not null && !filter.IsMatch(method.Name))
            {
                continue;
            }

            var parameters = string.Join(", ", method.GetParameters()
                .Select(p => $"{Short(p.ParameterType)} {p.Name}"));
            Console.WriteLine($"{MethodAccess(method)} {(method.IsStatic ? "static " : "")}{Short(method.ReturnType)} {method.Name}({parameters});");
        }

        return 0;
    }

    default:
        Console.Error.WriteLine($"Unknown command: {command}");
        return 1;
}

static IEnumerable<Type> EnumerateTypes(MetadataLoadContext mlc, IEnumerable<string> paths)
{
    foreach (var path in paths)
    {
        Assembly assembly;
        try
        {
            assembly = mlc.LoadFromAssemblyPath(path);
        }
        catch (BadImageFormatException)
        {
            continue;
        }

        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t is not null).ToArray()!;
        }

        foreach (var type in types)
        {
            yield return type;
        }
    }
}

static string FieldAccess(FieldInfo field) =>
    field.IsPublic ? "public" : field.IsFamily ? "protected" : field.IsAssembly ? "internal" : "private";

static string MethodAccess(MethodInfo method) =>
    method.IsPublic ? "public" : method.IsFamily ? "protected" : method.IsAssembly ? "internal" : "private";

static string Short(Type type) => type.IsGenericType
    ? $"{type.Name.Split('`')[0]}<{string.Join(", ", type.GetGenericArguments().Select(Short))}>"
    : type.Name;
