# ScreenInk

Aplicativo independente para Windows para capturar a tela e adicionar anotações, com uma barra flutuante compacta.

## Recursos

- Pincel, marca-texto, seta, retângulo, elipse, texto e borracha de anotações.
- Cores e espessura de 1 a 24 pixels, com indicador de tamanho variável.
- Desfazer/refazer, limpar e copiar imagem.
- Print da tela inteira ou de uma área selecionada, salvo como PNG.
- Lupa entre 100% e 400%.
- Shift para quadrados, círculos e alinhamento de setas em 45 graus.
- Minimizar, recolher/restaurar a barra e fechar.

## Compilar no Windows

Requer Windows com o compilador do .NET Framework 4.x em Framework64.

```powershell
powershell -ExecutionPolicy Bypass -File ./build.ps1
```

O executável fica em `dist/Pointofix-Modern.exe`.

## Usar

Abra o executável e escolha uma ferramenta para congelar a tela e começar a marcar. Esc encerra a captura; em espera, outro Esc fecha o aplicativo. O botão Print oferece tela inteira ou seleção de área antes de escolher onde salvar.

## Verificação

```powershell
Start-Process ./dist/Pointofix-Modern.exe -ArgumentList '--self-test',"$PWD/test-output.png" -Wait
Get-Content ./test-output.png.txt
```

Os testes cobrem renderização, histórico, proporções, zoom, espessura e recorte. Os novos diálogos de captura ainda precisam de uma verificação manual completa.

## Notas

Este projeto tem implementação própria e não inclui o executável ou código do Pointofix original. Os ícones de referência foram fornecidos pelo usuário; a procedência e a licença desses arquivos ainda não foram documentadas. Nenhuma licença de distribuição foi definida para o projeto.

