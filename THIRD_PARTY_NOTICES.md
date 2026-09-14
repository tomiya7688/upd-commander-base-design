# Third-Party Notices

UPD Commander Base Design本体はルートの `LICENSE` に記載されたMIT Licenseで提供されます。
配布されるチェッカーには、次の第三者コンポーネントが含まれる場合があります。

## Microsoft.CodeAnalysis.CSharp (Roslyn)

- 使用箇所: C# UPD Commander Checker
- バージョン: `4.11.0`
- ライセンス: MIT License
- 著作権者: .NET Foundation and Contributors
- ライセンス全文: [`licenses/ROSLYN-LICENSE.txt`](licenses/ROSLYN-LICENSE.txt)
- 上流: https://github.com/dotnet/roslyn

C#のself-contained配布物にはRoslynのアセンブリが含まれます。

## LLVM / Clang / libclang

- 使用箇所: C++ UPD Commander Checker
- ライセンス: Apache License 2.0 with LLVM Exceptions
- ライセンス全文: [`licenses/LLVM-LICENSE.txt`](licenses/LLVM-LICENSE.txt)
- 上流: https://github.com/llvm/llvm-project

Windows向けC++配布物へ `libclang.dll` をコピーした場合、このライセンス文書も同じ成果物へ収録されます。

各第三者コンポーネントは、それぞれのライセンス条件に従って提供されます。
