using System.Collections.Generic;

namespace RslParser.Types {
    /// <summary>
    /// 
    /// </summary>
    public class Book {
        /// <summary>
        /// Авторы книги
        /// </summary>
        public string Authors;
        
        /// <summary>
        /// Описание книги из блока "Карточка"
        /// </summary>
        public string Description;
        
        /// <summary>
        /// Ссылка на книгу
        /// </summary>
        public string Link;
        
        /// <summary>
        /// Буква по которой найдена книга
        /// </summary>
        public string Letter;
        
        /// <summary>
        /// Язык по которому найдена книга
        /// </summary>
        public string Lang;
        
        /// <summary>
        /// Страница, на которой найдена книга
        /// </summary>
        public int Page;
        
        /// <summary>
        /// Все поля из блока "Описание"
        /// </summary>
        public List<BookInfoValue> Values = new List<BookInfoValue>();
    }

    public class BookInfoValue {
        public string Name;
        public string Value;
    }
}
