CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_limpar_niveis_acessos`()
BEGIN
	-- 1. Desativa temporariamente as travas de segurança e integridade
	SET FOREIGN_KEY_CHECKS = 0;
	SET SQL_SAFE_UPDATES = 0;

	-- 2. Limpa a tabela usando DELETE (já que o TRUNCATE é totalmente bloqueado por FKs)
	DELETE FROM crud_central.niveisacessos;

	-- 3. Reativa todas as validações de segurança (BOAS PRÁTICAS)
	SET FOREIGN_KEY_CHECKS = 1;
	SET SQL_SAFE_UPDATES = 1;
END