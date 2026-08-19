using DotNetEnv;

namespace Infrastructure.Configuration;

public static class EnvironmentFile
{
    public static void Load() => Env.TraversePath().NoClobber().Load();
}
