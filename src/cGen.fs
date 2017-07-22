﻿module CGen

open System.IO

// Values to export in the C code (uniform and attribute values)
let exportedValues = ref ([] : (string * string * string) list)

// 'ty' is a prefix for the type of shader param. Nothing (VAR) for vars, "F" for hlsl functions
let export ty name (newName:string) =
  if newName.[0] <> '0' then
     exportedValues := !exportedValues |> List.map (fun (ty2, name2, newName2 as arg) ->
        if ty = ty2 && name = newName2 then ty, name2, newName
        else arg
     )
  else
    exportedValues := (ty, name, newName) :: !exportedValues

let private output() =
  if Ast.debugMode || Ast.outputName = "" then stdout
  else new StreamWriter(Ast.outputName) :> TextWriter

let printHeader data asAList =
  use out = output()
  let fileName =
      if Ast.outputName = "" then "shader_code.h"
      else Path.GetFileName Ast.outputName
  let macroName = fileName.Replace(".", "_").ToUpper() + "_"

  // meh
  // fprintfn out "// File generated with Shader Minifier %s http://www.ctrl-alt-test.fr, modifications by noby" Ast.version

  // would really like to remove this but crashes if taken out...
  fprintf out ""
  for file, code in data do
    let name = (Path.GetFileName file).Split('.').[0]
    fprintfn out "#pragma data_seg(\".shader\")"
    fprintfn out "const char* %s =\r\n \"%s\";" name (Printer.print code)
    
  //if not asAList then fprintfn out "#endif // %s" macroName

let printNoHeader data =
  use out = output()
  let str = [for _,code in data -> Printer.print code] |> String.concat "\n"
  fprintf out "%s" str

let printJSHeader data =
  use out = output()

  fprintfn out "/* File generated with Shader Minifier %s" Ast.version
  fprintfn out " * http://www.ctrl-alt-test.fr"
  fprintfn out " */"

  for ty, name, newName in List.sort !exportedValues do
    if ty = "" then
        fprintfn out "var var_%s = \"%s\"" (name.ToUpper()) newName
    else
        fprintfn out "var %c_%s = \"%s\"" (System.Char.ToUpper ty.[0]) (name.ToUpper()) newName

  fprintfn out ""
  for file, code in data do
    let name = (Path.GetFileName file).Replace(".", "_")
    fprintfn out "var %s =\r\n \"%s\"" name (Printer.print code)
    fprintfn out ""

let printNasmHeader data =
  use out = output()

  fprintfn out "; File generated with Shader Minifier %s" Ast.version
  fprintfn out "; http://www.ctrl-alt-test.fr"

  for ty, name, newName in List.sort !exportedValues do
    if ty = "" then
        fprintfn out "_var_%s: db '%s', 0" (name.ToUpper()) newName
    else
        fprintfn out "_%c_%s: db '%s', 0" (System.Char.ToUpper ty.[0]) (name.ToUpper()) newName

  fprintfn out ""
  for file, code in data do
    let name = (Path.GetFileName file).Replace(".", "_")
    fprintfn out "_%s:\r\n\tdb '%s', 0" name (Printer.print code)
    fprintfn out ""

let print data =
    match Ast.targetOutput with
    | Ast.Text -> printNoHeader data
    | Ast.CHeader -> printHeader data false
    | Ast.CList -> printHeader data true
    | Ast.JS -> printJSHeader data
    | Ast.Nasm -> printNasmHeader data
