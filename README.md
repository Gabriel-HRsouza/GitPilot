# GitPilot

Sistema em C# Windows Forms para automatizar Git e GitHub.

## Funções

- Selecionar pasta do projeto
- Inicializar Git
- Adicionar duas ou mais contas GitHub
- Escolher qual conta será usada no commit
- Rodar `git add .`
- Rodar `git commit`
- Rodar `git push`
- Criar repositório público ou privado no GitHub usando token
- Conectar repositório existente
- Ver arquivos alterados
- Ver logs dos comandos

## Visual

Tema vermelho, roxo e preto.

## Como abrir

1. Abra o Visual Studio
2. Clique em **Abrir projeto ou solução**
3. Selecione `GitPilot.csproj`
4. Execute com F5

## Antes de usar

Instale o Git no Windows e teste no terminal:

```bash
git --version
```

## Token do GitHub

Para criar repositório pelo app, gere um token no GitHub com permissão para criar repositórios.

Para apenas commitar e dar push, você pode conectar o repositório existente e usar a autenticação normal do Git/GitHub.
