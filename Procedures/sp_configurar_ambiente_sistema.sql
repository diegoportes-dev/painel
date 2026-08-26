CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_configurar_ambiente_sistema`(IN p_email_usuario VARCHAR(255))
BEGIN
    DECLARE v_perfil_id VARCHAR(36);
    DECLARE v_existe_usuario INT;

    -- 1. Tenta buscar o Id do Perfil Master
    SELECT Id INTO v_perfil_id 
    FROM Perfis 
    WHERE Nome = 'Administrador Geral'
    AND TenantId is Null
    LIMIT 1;

    -- Se o ID retornou NULL, o perfil não existe. Vamos criá-lo.
    IF v_perfil_id IS NULL THEN
        SET v_perfil_id = UUID();
        
        INSERT INTO Perfis (Id, Nome, Descricao, Ativo, Created, CreatedBy)
        VALUES (
            v_perfil_id, 
            'Administrador Geral', 
            'Suporte e Administração Global do Sistema', 
            'S', 
            UTC_TIMESTAMP(), 
            'Sistema'
        );
    END IF;

    -- 2. Garante a existência do Usuário informado no parâmetro
    SELECT COUNT(*) INTO v_existe_usuario 
    FROM Usuarios 
    WHERE Email = p_email_usuario;

    IF v_existe_usuario = 0 THEN
        INSERT INTO Usuarios (
            Id, Email, SenhaCrypt, PerfilId, TokenCadastro, 
            TokenCadastroExpiracao, Ativo, Created, CreatedBy
        )
        VALUES (
            UUID(), 
            p_email_usuario, 
            'nao_definida_ainda', 
            v_perfil_id, 
            'TOKEN123', 
            DATE_ADD(UTC_TIMESTAMP(), INTERVAL 7 DAY), 
            'S', 
            UTC_TIMESTAMP(), 
            'Sistema'
        );
    END IF;

END;