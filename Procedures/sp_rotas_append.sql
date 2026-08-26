CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_rotas_append`()
BEGIN
     
    SET @now = UTC_TIMESTAMP();
    SET @createdBy = 'Sistema';

    -- ====================================================
    -- 1. MÓDULO: AUTENTICAÇÃO (DESCOMENTADOS E PROTEGIDOS)
    -- ====================================================
    
    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'SetupCadastroSqlite') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'SetupCadastroSqlite', '/auth/setup-cadastro-sqlite', 'POST', 'HTTP: POST /auth/setup-cadastro-sqlite', 'Autenticação', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'SetupCadastro') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'SetupCadastro', '/auth/setup-cadastro', 'POST', 'HTTP: POST /auth/setup-cadastro', 'Autenticação', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'Login') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'Login', '/login', 'POST', 'HTTP: POST /login', 'Autenticação', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'EsqueciSenha') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'EsqueciSenha', '/login/esqueci-senha', 'POST', 'HTTP: POST /login/esqueci-senha', 'Autenticação', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'ResetarSenha') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'ResetarSenha', '/login/resetar-senha', 'POST', 'HTTP: POST /login/resetar-senha', 'Autenticação', @now, @createdBy);
    END IF;


    -- ====================================================
    -- 2. MÓDULO: CONFIGURAÇÕES (PERFIS DE USUÁRIO)
    -- ====================================================
    
    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'GetPerfis') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'GetPerfis', '/perfis', 'GET', 'HTTP: GET /perfis', 'Configurações', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'PostPerfis') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'PostPerfis', '/perfis', 'POST', 'HTTP: POST /perfis', 'Configurações', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'GetPerfisById') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'GetPerfisById', '/perfis/{id}', 'GET', 'HTTP: GET /perfis/{id}', 'Configurações', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'PutPerfisById') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'PutPerfisById', '/perfis/{id}', 'PUT', 'HTTP: PUT /perfis/{id}', 'Configurações', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'DeletePerfisById') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'DeletePerfisById', '/perfis/{id}', 'DELETE', 'HTTP: DELETE /perfis/{id}', 'Configurações', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'PatchPerfisById') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'PatchPerfisById', '/perfis/{id}', 'PATCH', 'HTTP: PATCH /perfis/{id}', 'Configurações', @now, @createdBy);
    END IF;


    -- ====================================================
    -- 3. MÓDULO: CADASTROS (USUÁRIOS)
    -- ====================================================
    
    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'GetUsuarios') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'GetUsuarios', '/usuarios', 'GET', 'HTTP: GET /usuarios', 'Cadastros', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'PostUsuarios') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'PostUsuarios', '/usuarios', 'POST', 'HTTP: POST /usuarios', 'Cadastros', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'GetUsuariosById') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'GetUsuariosById', '/usuarios/{id}', 'GET', 'HTTP: GET /usuarios/{id}', 'Cadastros', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'PutUsuariosById') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'PutUsuariosById', '/usuarios/{id}', 'PUT', 'HTTP: PUT /usuarios/{id}', 'Cadastros', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'DeleteUsuariosById') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'DeleteUsuariosById', '/usuarios/{id}', 'DELETE', 'HTTP: DELETE /usuarios/{id}', 'Cadastros', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'PatchUsuariosById') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'PatchUsuariosById', '/usuarios/{id}', 'PATCH', 'HTTP: PATCH /usuarios/{id}', 'Cadastros', @now, @createdBy);
    END IF;


    -- ====================================================
    -- 4. MÓDULO: ADMINISTRAÇÃO (TENANTS / EMPRESAS)
    -- ====================================================
    
    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'GetTenants') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'GetTenants', '/tenants', 'GET', 'HTTP: GET /tenants', 'Administração', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'PostTenants') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'PostTenants', '/tenants', 'POST', 'HTTP: POST /tenants', 'Administração', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'GetTenantsById') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'GetTenantsById', '/tenants/{id}', 'GET', 'HTTP: GET /tenants/{id}', 'Administração', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'PutTenantsById') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'PutTenantsById', '/tenants/{id}', 'PUT', 'HTTP: PUT /tenants/{id}', 'Administração', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'DeleteTenantsById') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'DeleteTenantsById', '/tenants/{id}', 'DELETE', 'HTTP: DELETE /tenants/{id}', 'Administração', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'PatchTenantsById') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'PatchTenantsById', '/tenants/{id}', 'PATCH', 'HTTP: PATCH /tenants/{id}', 'Administração', @now, @createdBy);
    END IF;


    -- ====================================================
    -- 5. MÓDULO: NÍVEIS DE ACESSO (MATRIZ / NOVAS ROTAS)
    -- ====================================================
    
    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'GetNiveisAcesso') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'GetNiveisAcesso', '/niveis-acesso', 'GET', 'HTTP: GET /niveis-acesso', 'Níveis de Acesso', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'PostNiveisAcesso') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'PostNiveisAcesso', '/niveis-acesso', 'POST', 'HTTP: POST /niveis-acesso', 'Níveis de Acesso', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'GetNiveisAcessoById') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'GetNiveisAcessoById', '/niveis-acesso/{id}', 'GET', 'HTTP: GET /niveis-acesso/{id}', 'Níveis de Acesso', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'PutNiveisAcessoById') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'PutNiveisAcessoById', '/niveis-acesso/{id}', 'PUT', 'HTTP: PUT /niveis-acesso/{id}', 'Níveis de Acesso', @now, @createdBy);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM rotas WHERE EndPoint = 'DeleteNiveisAcessoById') THEN
        INSERT INTO rotas (Id, EndPoint, Rota, Metodo, Descricao, Menu, Created, CreatedBy)
        VALUES (UUID(), 'DeleteNiveisAcessoById', '/niveis-acesso/{id}', 'DELETE', 'HTTP: DELETE /niveis-acesso/{id}', 'Níveis de Acesso', @now, @createdBy);
    END IF;
END;
