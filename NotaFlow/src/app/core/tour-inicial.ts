import { TourStep } from './models/tour-step';

export const TOUR_INICIAL: readonly TourStep[] = [
  {
    id: 'painel',
    route: '/painel',
    anchor: '#cartoes-resumo',
    title: 'Comece pelo panorama',
    text: 'O painel resume o sistema: quantos produtos existem, quantas unidades há em estoque, e em que situação estão as notas. Tudo vem das mesmas listas que você vai usar a seguir.'
  },
  {
    id: 'produtos',
    route: '/produtos',
    anchor: '#nav-caixa',
    title: 'Tudo começa no estoque',
    text: 'Uma nota só aceita produtos que já existem. Esta área é a dona do saldo: nenhuma outra parte do sistema altera esse número.'
  },
  {
    id: 'novo-produto',
    route: '/produtos',
    anchor: '#btn-novo-produto',
    title: 'Cadastre o primeiro',
    text: 'Informe código, descrição e um saldo inicial. Ao salvar, o produto é enviado por mensagem para o faturamento — por isso ele aparece na nota alguns instantes depois.'
  },
  {
    id: 'notas',
    route: '/notas',
    anchor: '#nav-documento',
    title: 'Agora a nota fiscal',
    text: 'Repare que a identidade muda: azul e serifada, porque aqui é o escritório. Em Produtos era laranja e monoespaçada, o chão de fábrica.'
  },
  {
    id: 'nova-nota',
    route: '/notas',
    anchor: '#btn-nova-nota',
    title: 'Abra, inclua, feche',
    text: 'A nota nasce vazia e com número sequencial. Dentro dela você busca produtos, define quantidade e fecha a nota — é o fechamento que dá baixa no estoque. Depois de fechada, dá para imprimir o PDF.'
  },
  {
    id: 'assistente',
    route: '/notas',
    title: 'O assistente de IA ✨',
    text: 'Dentro de uma nota aberta há um campo roxo com estrela. Escreva "3 parafusos sextavados e dois martelos" e a IA resolve os itens contra o catálogo. Tudo que vem de IA é roxo e marcado — ela propõe, você confirma. Ela nunca grava sozinha, e nunca inventa produto que não exista.'
  },
  {
    id: 'situacao',
    route: '/notas',
    anchor: '#filtro-situacao',
    title: 'Quatro situações',
    text: 'Aberta é editável. Processando está esperando o estoque. Pendente foi recusada e voltou a ser editável. Fechada já deu baixa e não muda mais.'
  },
  {
    id: 'usuarios',
    route: '/usuarios',
    anchor: '#nav-pessoa',
    title: 'Quem vê o quê',
    text: 'Administrador e Gerente enxergam as notas de todos e podem cadastrar produto. Funcionário só vê as próprias notas. Crie uma conta aqui e teste numa janela anônima.'
  },
  {
    id: 'falha',
    title: 'Por último, quebre de propósito',
    text: 'Rode "docker compose --profile app stop estoque", feche uma nota e suba o serviço de volta. A nota se resolve sozinha na tela, sem você tocar em nada. É o que este sistema tem de mais interessante.'
  }
];
