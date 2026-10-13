# ScreenInk

Ferramenta de anotação e captura de tela para Windows, com barra flutuante moderna.

## Baixar

[Instalador 0.2.0](https://github.com/arilsonbc/screenink/releases/download/v0.2.0/ScreenInk-Setup-0.2.0.exe)

Requer Windows 10/11 e .NET Framework 4.7.2 ou superior. O instalador é para o
usuário atual, oferece atalho e desinstalação e ainda não possui assinatura digital.

## Recursos

Pincel, marca-texto, linhas e setas, retângulos, elipses e 16 formas adicionais;
preenchimento, transparência e estilos de traço; seleção e movimentação de anotações;
texto configurável; conta-gotas HEX/RGB; régua em pixels, transferidor e guias;
ponteiro e holofote; lupa até 400%; quadros, imagens e múltiplas páginas;
projeto editável, PNG e PDF; captura inteira, retangular e livre;
barra personalizável e atalhos remapeáveis.

Abra **Mais ferramentas** para acessar as novas opções. Veja [o guia](COMO-USAR.txt),
a [comparação de recursos](FEATURES.md) e o [relatório de testes](TESTS.md).

## Compilar

No Windows com o compilador .NET Framework 4.x:

```powershell
powershell -ExecutionPolicy Bypass -File ./build.ps1
```

O executável será criado em `app/ScreenInk-0.2.0-final.exe`.
Para gerar o instalador, compile `ScreenInk.iss` com Inno Setup 6.7.3.

## Testes

```powershell
Start-Process ./app/ScreenInk-0.2.0-final.exe -ArgumentList '--self-test',"$PWD/test-output.png" -Wait
Get-Content ./test-output.png.txt
Get-Content ./test-output.advanced.txt
```

Os testes dos diálogos abrem janelas temporárias. São verificadas também as formas,
as páginas, o projeto editável, captura livre e a geração de PDF.

## Notas

Implementação própria: não inclui código nem o executável do Pointofix ou do
SwordSoft Screenink. O PDF contém páginas rasterizadas; o projeto `.screenink`
preserva edição e exige a resolução de origem. Máximo de 32 páginas por projeto.
Os ícones e imagens de marca foram fornecidos pelo usuário; suas licenças e
procedência ainda não estão documentadas. Não foi definida uma licença do projeto.

