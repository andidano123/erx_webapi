
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ERX.Services.Providers.Factory
{
  public sealed class ProxyFactory
  {
    private static Dictionary<string, ProxyFactory.CreateInstanceHandler> m_Handlers = new Dictionary<string, ProxyFactory.CreateInstanceHandler>();

    private ProxyFactory()
    {
    }

    private static void CreateHandler(Type objtype, string key, Type[] ptypes)
    {
      lock (typeof (ProxyFactory))
      {
        if (ProxyFactory.m_Handlers.ContainsKey(key))
          return;
        DynamicMethod dynamicMethod = new DynamicMethod(key, typeof (object), new Type[1]
        {
          typeof (object[])
        }, typeof (ProxyFactory).Module);
        ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
        ConstructorInfo constructor = objtype.GetConstructor(ptypes);
        ilGenerator.Emit(OpCodes.Nop);
        for (int index = 0; index < ptypes.Length; ++index)
        {
          ilGenerator.Emit(OpCodes.Ldarg_0);
          ilGenerator.Emit(OpCodes.Ldc_I4, index);
          ilGenerator.Emit(OpCodes.Ldelem_Ref);
          if (ptypes[index].IsValueType)
            ilGenerator.Emit(OpCodes.Unbox_Any, ptypes[index]);
          else
            ilGenerator.Emit(OpCodes.Castclass, ptypes[index]);
        }
        ilGenerator.Emit(OpCodes.Newobj, constructor);
        ilGenerator.Emit(OpCodes.Ret);
        ProxyFactory.CreateInstanceHandler createInstanceHandler = (ProxyFactory.CreateInstanceHandler) dynamicMethod.CreateDelegate(typeof (ProxyFactory.CreateInstanceHandler));
        ProxyFactory.m_Handlers.Add(key, createInstanceHandler);
      }
    }

    public static T CreateInstance<T>()
    {
      return ProxyFactory.CreateInstance<T>((object[]) null);
    }

    public static T CreateInstance<T>(params object[] parameters)
        {
      Type objtype = typeof (T);
      Type[] parameterTypes = ProxyFactory.GetParameterTypes(parameters);
      string key = typeof (T).FullName + "_" + ProxyFactory.GetKey(parameterTypes);
      if (!ProxyFactory.m_Handlers.ContainsKey(key))
        ProxyFactory.CreateHandler(objtype, key, parameterTypes);
      return (T) ProxyFactory.m_Handlers[key](parameters);
    }

    private static string GetKey(params Type[] types)
    {
      if (types == null || types.Length == 0)
        return "null";
      return string.Concat((object[]) types);
    }

    private static Type[] GetParameterTypes(params object[] parameters)
    {
      if (parameters == null)
        return new Type[0];
      Type[] typeArray = new Type[parameters.Length];
      for (int index = 0; index < parameters.Length; ++index)
        typeArray[index] = parameters[index].GetType();
      return typeArray;
    }

    public delegate object CreateInstanceHandler(object[] parameters);
  }
}
