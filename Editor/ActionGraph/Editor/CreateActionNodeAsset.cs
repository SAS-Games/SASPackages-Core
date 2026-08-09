using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

public class CreateActionNodeAsset : EndNameEditAction
{
    public override void Action(int instanceId, string pathName, string resourceFile)
    {
        string className = Path.GetFileNameWithoutExtension(pathName);
        string providerName = className + "Provider";

        string template = $@"using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[NodeBinding(typeof({className}))]
[Serializable]
public class {providerName} : ActionDataProvider<EmptyData>
{{
}}

public class {className} : ActionNode<EmptyData>
{{
    public {className}(ActionDataProvider<EmptyData> dataProvider) : base(dataProvider)
    {{
    }}

    public override async Task ExecuteAsync(ActionContext context, CancellationToken token)
    {{
        token.ThrowIfCancellationRequested();

        try
        {{
            // TODO: Implement logic here

            await Task.CompletedTask;
        }}
        catch (OperationCanceledException)
        {{
            throw;
        }}
    }}
}}
";

        File.WriteAllText(pathName, template);
        AssetDatabase.ImportAsset(pathName);

        var asset = AssetDatabase.LoadAssetAtPath<Object>(pathName);
        ProjectWindowUtil.ShowCreatedAsset(asset);
    }
}