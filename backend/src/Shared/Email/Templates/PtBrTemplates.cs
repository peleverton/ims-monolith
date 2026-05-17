namespace IMS.Modular.Shared.Email.Templates;

/// <summary>
/// US-085: PT-BR email template strings.
/// </summary>
internal static class PtBrTemplates
{
    public static readonly Dictionary<string, (string Subject, string Body)> Templates = new()
    {
        [nameof(EmailTemplateType.IssueCreated)] = (
            Subject: "Nova Issue Criada: {Title}",
            Body: """
                <html><body>
                <h2>Nova Issue Criada</h2>
                <p><strong>Título:</strong> {Title}</p>
                <p><strong>Prioridade:</strong> {Priority}</p>
                <p><strong>Descrição:</strong> {Description}</p>
                <p>Acesse o sistema para mais detalhes.</p>
                </body></html>
                """),
        [nameof(EmailTemplateType.LowStock)] = (
            Subject: "Alerta de Estoque Baixo: {ProductName}",
            Body: """
                <html><body>
                <h2>Alerta de Estoque Baixo</h2>
                <p><strong>Produto:</strong> {ProductName}</p>
                <p><strong>SKU:</strong> {Sku}</p>
                <p><strong>Estoque atual:</strong> {CurrentStock}</p>
                <p><strong>Estoque mínimo:</strong> {MinimumStock}</p>
                <p>Por favor, providencie a reposição do estoque.</p>
                </body></html>
                """),
        [nameof(EmailTemplateType.Welcome)] = (
            Subject: "Bem-vindo ao IMS, {FullName}!",
            Body: """
                <html><body>
                <h2>Bem-vindo ao Sistema de Gestão (IMS)!</h2>
                <p>Olá, <strong>{FullName}</strong>!</p>
                <p>Sua conta foi criada com sucesso.</p>
                <p><strong>Usuário:</strong> {Username}</p>
                <p>Faça login para começar a usar o sistema.</p>
                </body></html>
                """)
    };
}
