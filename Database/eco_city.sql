-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Tempo de geração: 03/12/2025 às 03:23
-- Versão do servidor: 10.4.32-MariaDB
-- Versão do PHP: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Banco de dados: `eco_city`
--

-- --------------------------------------------------------

--
-- Estrutura para tabela `accounts`
--

CREATE TABLE `accounts` (
  `id` int(11) NOT NULL,
  `name` varchar(300) NOT NULL DEFAULT 'Player',
  `device_id` varchar(500) NOT NULL,
  `gems` int(11) NOT NULL DEFAULT 0,
  `is_online` int(1) NOT NULL DEFAULT 0,
  `client_id` int(11) NOT NULL DEFAULT 0,
  `trophies` int(11) NOT NULL DEFAULT 0,
  `banned` int(1) NOT NULL DEFAULT 0,
  `shield` datetime NOT NULL DEFAULT current_timestamp(),
  `xp` int(11) NOT NULL DEFAULT 0,
  `level` int(11) NOT NULL DEFAULT 1,
  `global_chat_blocked` int(1) NOT NULL DEFAULT 0,
  `last_chat` datetime NOT NULL DEFAULT current_timestamp(),
  `chat_color` varchar(50) NOT NULL DEFAULT '',
  `email` varchar(1000) NOT NULL DEFAULT '',
  `password` varchar(1000) NOT NULL DEFAULT '',
  `map_layout` int(3) NOT NULL DEFAULT 0,
  `shld_cldn_1` datetime NOT NULL DEFAULT current_timestamp(),
  `shld_cldn_2` datetime NOT NULL DEFAULT current_timestamp(),
  `shld_cldn_3` datetime NOT NULL DEFAULT current_timestamp(),
  `last_login` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

-- --------------------------------------------------------

--
-- Estrutura para tabela `buildings`
--

CREATE TABLE `buildings` (
  `id` int(11) NOT NULL,
  `global_id` varchar(50) NOT NULL DEFAULT '',
  `account_id` int(11) NOT NULL,
  `level` int(11) NOT NULL DEFAULT 0,
  `x_position` int(11) NOT NULL DEFAULT 0,
  `y_position` int(11) NOT NULL DEFAULT 0,
  `gold_storage` float NOT NULL DEFAULT 0,
  `elixir_storage` float NOT NULL DEFAULT 0,
  `dark_elixir_storage` float NOT NULL DEFAULT 0,
  `boost` datetime NOT NULL DEFAULT current_timestamp(),
  `construction_time` datetime NOT NULL DEFAULT current_timestamp(),
  `is_constructing` int(1) NOT NULL DEFAULT 0,
  `construction_build_time` int(11) NOT NULL DEFAULT 0,
  `track_time` datetime NOT NULL DEFAULT current_timestamp(),
  `x_war` int(11) NOT NULL DEFAULT -1,
  `y_war` int(11) NOT NULL DEFAULT -1
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

-- --------------------------------------------------------

--
-- Estrutura para tabela `chat_messages`
--

CREATE TABLE `chat_messages` (
  `id` int(11) NOT NULL,
  `account_id` int(11) NOT NULL DEFAULT 0,
  `type` int(11) NOT NULL DEFAULT 0,
  `global_id` int(11) NOT NULL DEFAULT 0,
  `clan_id` int(11) NOT NULL DEFAULT 0,
  `message` varchar(1000) NOT NULL,
  `send_time` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

--
-- Despejando dados para a tabela `chat_messages`
--

INSERT INTO `chat_messages` (`id`, `account_id`, `type`, `global_id`, `clan_id`, `message`, `send_time`) VALUES
(74, 863, 1, 0, 0, 'b2k=', '2025-08-25 19:21:35'),
(75, 863, 1, 0, 0, 'c2FsdmU=', '2025-08-25 19:21:47'),
(76, 880, 1, 0, 0, 'b2ll', '2025-11-14 23:16:53'),
(77, 884, 1, 0, 0, 'YmxhYmxhdmJsYQ==', '2025-11-15 10:52:21');

-- --------------------------------------------------------

--
-- Estrutura para tabela `chat_reports`
--

CREATE TABLE `chat_reports` (
  `id` int(11) NOT NULL,
  `message_id` int(11) NOT NULL,
  `reporter_id` int(11) NOT NULL,
  `target_id` int(11) NOT NULL,
  `message` varchar(500) NOT NULL,
  `report_time` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

-- --------------------------------------------------------

--
-- Estrutura para tabela `iap`
--

CREATE TABLE `iap` (
  `id` int(11) NOT NULL,
  `account_id` int(11) NOT NULL,
  `market` varchar(100) NOT NULL,
  `product_id` varchar(100) NOT NULL,
  `token` varchar(100) NOT NULL,
  `save_time` datetime NOT NULL DEFAULT current_timestamp(),
  `price` varchar(100) NOT NULL DEFAULT '0',
  `currency` varchar(50) NOT NULL DEFAULT 'USD',
  `validated` int(1) NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

-- --------------------------------------------------------

--
-- Estrutura para tabela `research`
--

CREATE TABLE `research` (
  `id` int(11) NOT NULL,
  `type` int(11) NOT NULL,
  `account_id` int(11) NOT NULL,
  `global_id` varchar(100) NOT NULL,
  `level` int(11) NOT NULL DEFAULT 1,
  `researching` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

-- --------------------------------------------------------

--
-- Estrutura para tabela `server_buildings`
--

CREATE TABLE `server_buildings` (
  `id` int(11) NOT NULL,
  `global_id` varchar(50) NOT NULL DEFAULT '',
  `level` int(11) NOT NULL DEFAULT 0,
  `req_gold` int(11) NOT NULL DEFAULT 0,
  `req_elixir` int(11) NOT NULL DEFAULT 0,
  `req_gems` int(11) NOT NULL DEFAULT 0,
  `req_dark_elixir` int(11) NOT NULL DEFAULT 0,
  `columns_count` int(11) NOT NULL DEFAULT 0,
  `rows_count` int(11) NOT NULL DEFAULT 0,
  `capacity` int(11) NOT NULL DEFAULT 0,
  `gold_capacity` int(11) NOT NULL DEFAULT 0,
  `elixir_capacity` int(11) NOT NULL DEFAULT 0,
  `dark_elixir_capacity` int(11) NOT NULL DEFAULT 0,
  `speed` float NOT NULL DEFAULT 0,
  `health` int(11) NOT NULL DEFAULT 100,
  `damage` float NOT NULL DEFAULT 0,
  `target_type` varchar(50) NOT NULL DEFAULT 'none',
  `radius` float NOT NULL DEFAULT 0,
  `blind_radius` float NOT NULL DEFAULT 0,
  `splash_radius` float NOT NULL DEFAULT 0,
  `projectile_speed` float NOT NULL DEFAULT 0,
  `build_time` int(11) NOT NULL DEFAULT 0,
  `gained_xp` int(11) NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

--
-- Despejando dados para a tabela `server_buildings`
--

INSERT INTO `server_buildings` (`id`, `global_id`, `level`, `req_gold`, `req_elixir`, `req_gems`, `req_dark_elixir`, `columns_count`, `rows_count`, `capacity`, `gold_capacity`, `elixir_capacity`, `dark_elixir_capacity`, `speed`, `health`, `damage`, `target_type`, `radius`, `blind_radius`, `splash_radius`, `projectile_speed`, `build_time`, `gained_xp`) VALUES
(1, 'townhall', 1, 0, 0, 0, 0, 4, 4, 0, 1000, 1000, 0, 0, 450, 0, 'none', 0, 0, 0, 0, 0, 0),
(16, 'goldmine', 1, 100, 0, 0, 0, 3, 3, 0, 1000, 0, 0, 200, 400, 0, 'none', 0, 0, 0, 0, 10, 3),
(31, 'elixirmine', 1, 100, 0, 0, 0, 3, 3, 0, 0, 1000, 0, 200, 400, 0, 'none', 0, 0, 0, 0, 10, 3),
(46, 'goldstorage', 1, 100, 0, 0, 0, 3, 3, 0, 1500, 0, 0, 0, 400, 0, 'none', 0, 0, 0, 0, 10, 3),
(62, 'elixirstorage', 1, 300, 0, 0, 0, 3, 3, 0, 0, 1500, 0, 0, 400, 0, 'none', 0, 0, 0, 0, 10, 3),
(112, 'armycamp', 1, 200, 0, 0, 0, 4, 4, 20, 0, 0, 0, 0, 250, 0, 'none', 0, 0, 0, 0, 10, 17),
(148, 'laboratory', 1, 300, 0, 0, 0, 3, 3, 0, 0, 0, 0, 0, 500, 0, 'none', 0, 0, 0, 0, 20, 7),
(161, 'spellfactory', 1, 300, 0, 0, 0, 3, 3, 2, 0, 0, 0, 0, 425, 0, 'none', 0, 0, 0, 0, 20, 169),
(303, 'obstacle', 1, 50, 0, 0, 0, 2, 2, 0, 0, 0, 0, 0, 100, 0, 'none', 0, 0, 0, 0, 10, 0),
(309, 'tree', 1, 50, 0, 0, 0, 2, 2, 0, 0, 0, 0, 0, 100, 0, 'none', 0, 0, 0, 0, 10, 0);

-- --------------------------------------------------------

--
-- Estrutura para tabela `verification_codes`
--

CREATE TABLE `verification_codes` (
  `id` int(11) NOT NULL,
  `target` varchar(1000) NOT NULL,
  `device_id` varchar(1000) NOT NULL,
  `code` varchar(50) NOT NULL,
  `expire_time` datetime NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

--
-- Índices para tabelas despejadas
--

--
-- Índices de tabela `accounts`
--
ALTER TABLE `accounts`
  ADD PRIMARY KEY (`id`);

--
-- Índices de tabela `buildings`
--
ALTER TABLE `buildings`
  ADD PRIMARY KEY (`id`);

--
-- Índices de tabela `chat_messages`
--
ALTER TABLE `chat_messages`
  ADD PRIMARY KEY (`id`);

--
-- Índices de tabela `chat_reports`
--
ALTER TABLE `chat_reports`
  ADD PRIMARY KEY (`id`);

--
-- Índices de tabela `iap`
--
ALTER TABLE `iap`
  ADD PRIMARY KEY (`id`);

--
-- Índices de tabela `research`
--
ALTER TABLE `research`
  ADD PRIMARY KEY (`id`);

--
-- Índices de tabela `server_buildings`
--
ALTER TABLE `server_buildings`
  ADD PRIMARY KEY (`id`);

--
-- Índices de tabela `verification_codes`
--
ALTER TABLE `verification_codes`
  ADD PRIMARY KEY (`id`);

--
-- AUTO_INCREMENT para tabelas despejadas
--

--
-- AUTO_INCREMENT de tabela `accounts`
--
ALTER TABLE `accounts`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=939;

--
-- AUTO_INCREMENT de tabela `buildings`
--
ALTER TABLE `buildings`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=110278;

--
-- AUTO_INCREMENT de tabela `chat_messages`
--
ALTER TABLE `chat_messages`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=78;

--
-- AUTO_INCREMENT de tabela `chat_reports`
--
ALTER TABLE `chat_reports`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=21;

--
-- AUTO_INCREMENT de tabela `iap`
--
ALTER TABLE `iap`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=16;

--
-- AUTO_INCREMENT de tabela `research`
--
ALTER TABLE `research`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=38;

--
-- AUTO_INCREMENT de tabela `server_buildings`
--
ALTER TABLE `server_buildings`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=310;

--
-- AUTO_INCREMENT de tabela `verification_codes`
--
ALTER TABLE `verification_codes`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=30;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
