using ERX.Services.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Http.ModelBinding;

public static class Extensions
{
    /// <summary>
    /// </summary>
    /// <param name="content"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    private static byte[] XorEncrypt(byte[] content, byte[] keydata)
    {
        for (int i = 0; i < content.Length; i++)
        {
            content[i] ^= keydata[i % keydata.Length];
        }
        return content;
    }
    
    public static DbResult<string> ToDbResult(this ModelStateDictionary modelState)
    {
        var error = "";
        var list = modelState.Values.Where(s => s.Errors.Count > 0).ToList();
        if (list.Any())
        {
            error = list.First().Errors.Select(e => e.ErrorMessage).First();
        }
        return new DbResult<string>()
        {
            IsSuccess = modelState.IsValid,
            Data = "",
            ErrorMessage = error
        };
    }
}
