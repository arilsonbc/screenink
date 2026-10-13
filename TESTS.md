# Verificação da versão 0.2.0

Resultados: testes concluídos em 13/10/2026.

- Renderização de anotações, desfazer/refazer e restauração após limpar.
- Proporções com Shift nos quatro quadrantes e alinhamento de setas.
- Limites da espessura e zoom; amostragem de imagem ampliada em 400%.
- 16 formas adicionais, geometria e pixels de preenchimento.
- Distâncias em pixels e conversão angular.
- Duplicação de páginas sem compartilhar anotações mutáveis; exclusão e quadros.
- Projeto ZIP/XML editável salvo e reaberto preservando formas e preenchimentos.
- Captura livre com transparência fora do contorno e validação de áreas vazias.
- Preferências, ação Salvar e renderização das quatro abas de configurações.
- Navegador de páginas: miniaturas, botões Duplicar/Excluir e renderização.
- Diálogos Print (inteira/retângulo/livre) e conta-gotas renderizados e inspecionados.
- PDF de duas páginas: estrutura estrita, imagens e renderização visual verificados.
- Instalador de validação com AppId separado: instalação, hash do executável,
  testes da cópia instalada e desinstalação bem-sucedidos.

Correções encontradas durante os testes:
- Miniaturas não podem ser descartadas antes da criação do controle nativo.
- Atualização da barra ignora janelas descartadas.
- Atalhos remapeados não executam também os atalhos antigos.
- Esc encerra a captura em uma tecla, inclusive nas ferramentas de apresentação.
- Barra vertical calcula colunas suficientes para caber na tela.

Limites:
- Esta comparação cobre recursos mostrados na página oficial e suas imagens,
  sem afirmar equivalência com funcionalidades internas não documentadas.
- O PDF preserva páginas como imagens; .screenink preserva as anotações editáveis.
- Réguas medem pixels de tela; projetos editáveis exigem a resolução de origem.
- A versão não tem assinatura digital. Testes não garantem ausência de falhas
  em outros computadores, escalas de DPI ou combinações de monitores.

