﻿// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

open MathNet.Numerics.LinearAlgebra
open System
open System.IO
open NeuralNet

[<EntryPoint>]
let main argv = 
    let sizes = [784;30;10]

    let resultVectors = [
        DenseVector.ofList [1.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0];
        DenseVector.ofList [0.0; 1.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0];
        DenseVector.ofList [0.0; 0.0; 1.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0];
        DenseVector.ofList [0.0; 0.0; 0.0; 1.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0];
        DenseVector.ofList [0.0; 0.0; 0.0; 0.0; 1.0; 0.0; 0.0; 0.0; 0.0; 0.0];
        DenseVector.ofList [0.0; 0.0; 0.0; 0.0; 0.0; 1.0; 0.0; 0.0; 0.0; 0.0];
        DenseVector.ofList [0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 1.0; 0.0; 0.0; 0.0];
        DenseVector.ofList [0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 1.0; 0.0; 0.0];
        DenseVector.ofList [0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 1.0; 0.0];
        DenseVector.ofList [0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 0.0; 1.0]];

    let files = [ // currently throws stackoverflow on using complete dataset
        "../../../teach-data/0.hex";
        "../../../teach-data/1.hex";
        "../../../teach-data/2.hex";
        "../../../teach-data/3.hex";
        "../../../teach-data/4.hex";
        "../../../teach-data/5.hex";
        "../../../teach-data/6.hex";
        "../../../teach-data/7.hex";
        "../../../teach-data/8.hex";
        "../../../teach-data/9.hex"]

    printfn "Building examples..."
    
    let rec buildExamples(rs : Vector<double> list, fs : string list) = 
        if rs.Length = 0 then List.empty else
        let reader = new BinaryReader(File.Open(fs.Head, FileMode.Open))

        let toDouble(b1 : int, b2 : int) : double =
            let hex : string = new string([(char)(b1);(char)(b2)] |> Array.ofList)
            (double)(System.Int32.Parse(hex, System.Globalization.NumberStyles.AllowHexSpecifier))

        let readNextMNIST(br : BinaryReader) =
            DenseVector.ofList [for i in 1 .. 784 do yield toDouble(reader.Read(),reader.Read())]
        let images = (int)(reader.BaseStream.Length) / (784 * 2)
        let digits = [for i in 1 .. images do yield (readNextMNIST(reader), rs.Head)]

        List.append digits (buildExamples (rs.Tail, fs.Tail))

    let examples = buildExamples(resultVectors, files)

    printfn "Starting to teach %d examples..." examples.Length

    let agent : MailboxProcessor<string> = MailboxProcessor.Start(fun inbox ->
        let rec messageLoop = async {
            let! msg = inbox.Receive()
            printfn "%s" msg
            return! messageLoop
        }

        messageLoop
    )

    let network = Network.Randomize(sizes, Some(agent))
    let (w, b) = network.Teach(examples, 3.0, 10, 1, examples)
    
    let mutable filename = "output.cs"
    if argv.Length = 1 then
        filename <- argv.[0]

    // still needs a bit of cleanup before it's clean C# code
    let bw = new StreamWriter(File.Open(filename, FileMode.Create))
    bw.Write("List<Vector<double>> biases = new List<Vector<double>>() {\n")
    
    let rec writeVectorString(vects : Vector<double> list) =
        if vects.Length = 0 then bw.Write("}\n") else
        let vs = vects.Head.ToVectorString(System.Int32.MaxValue, 1, "F3")
        let line = "{" + vs.Replace("\r\n", ",")
        if vects.Length = 1 then 
            bw.Write(line.Substring(0, line.Length - 2) + "}\n")
        else
            bw.Write(line.Substring(0, line.Length - 2) + "}, \n")
        writeVectorString(vects.Tail)

    writeVectorString(b)

    bw.Write("List<Matrix<double>> weights = new List<Matrix<double>>() {\n")

    let rec writeMatrixString(m : Matrix<double> list) =
        if m.Length = 0 then bw.Write("};\n") else

        bw.Write("Matrix<double>.Build.DenseOfArray(new [,] {\n")
        let listOfVects = [for row in m.Head.ToRowArrays() do yield DenseVector.ofArray(row)]
        writeVectorString(listOfVects)

        if m.Length = 1 then 
            bw.Write("}\n")
        else
            bw.Write("}), \n")
        writeMatrixString(m.Tail)

    writeMatrixString(w)

    printfn "Press enter to exit..."
    let x = Console.ReadLine()
    
    0 // return an integer exit code
