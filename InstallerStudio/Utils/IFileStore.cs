using System;
using System.Collections.Generic;

namespace InstallerStudio.Utils
{
  /// <summary>
  /// Состояния файлового хранилища.
  /// </summary>
  enum FileStoreState { Changed, Saved }

  /// <summary>
  /// Файловое хранилище.
  /// Создание и открытие хранилища делается через конструктор.
  /// </summary>
  /************************************************
   * Create() =======> FileStoreState.Changed     *
   *                      /\            ||        *
   *                      ||            ||        *
   *             Change() ||            || Save() *
   *                      ||            ||        *
   *                      ||            \/        *
   * Open()   =======> FileStoreState.Saved       *
   ************************************************/
  interface IFileStore : IDisposable
  {
    /// <summary>
    /// Путь к хранилищу.
    /// </summary>
    string StoreDirectory { get; }
    /// <summary>
    /// Список файлов в хранилище. Содержит относительный путь.
    /// </summary>
    IReadOnlyList<string> Files { get; }
    /// <summary>
    /// Сохранение хранилища в файл.
    /// </summary>
    /// <param name="path"></param>
    void Save(string path);
    /// <summary>
    /// Добавить файл к хранилищу.
    /// </summary>
    /// <param name="path">Реальный путь к файлу.</param>
    /// <param name="relativePath">Путь к файлу в хранилище.</param>
    void AddFile(string path, string relativePath);
    /// <summary>
    /// Удалить файл из хранилища.
    /// </summary>
    /// <param name="relativePath">Относительный путь к удаляемому файлу в хранилище.</param>
    void DeleteFile(string relativePath);
    /// <summary>
    /// Замена файла в хранилище.
    /// </summary>
    /// <param name="relativePath">Относительный путь к заменяемому файлу в хранилище.</param>
    /// <param name="path">Путь к новому файлу.</param>
    void ReplaceFile(string relativePath, string path);
    /// <summary>
    /// Состояние файлового хранилища.
    /// </summary>
    FileStoreState State { get; }
  }
}
