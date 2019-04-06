import scrapy

class ImdbSpider(scrapy.Spider):
    name = 'imdbspider'
    #Здесь нужно или получить массив фильмов из бд\файла, или получить параметр из командной строки
    #и собрать URI
    #Для примера подойдёт и константа
    start_urls = ['https://www.imdb.com/title/tt0113243/']

    def parse(self, response):
        for title in response.css('.title_wrapper>h1'):
            yield {'title': title.css('h1 ::text').get().replace(u'\xa0', u'')}
            yield {'year': title.css('a ::text').get()}

        #Пример xpath селекторов
        for rating in response.css('.ratingValue>strong>span'):
            yield {'rating': rating.css('span ::text').get()}
            yield {'ratingXPath': rating.xpath('string(.)').extract()}

        for plot_page in response.css('#titleStoryLine > span:nth-child(4) > a:nth-child(3)'):
            #Пример перехода по ссылкам
            yield response.follow(plot_page, self.parseSyno)#Если ссылка найдётся то мы переёдм по ней и вызовем метод parsSyno

    def parseSyno(self, response):
        for syno in response.css('#plot-synopsis-content>li'):
            yield {'synopsys': syno.xpath('text()').extract()}
~
