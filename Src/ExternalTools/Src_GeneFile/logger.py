import logging
import inspect
import os
""" Pythonのloggingに[ファイル名：行番号　メソッド名]を出力できるようにしたい
https://qiita.com/hubuzo/items/27156a470105bb07bfc4 を参考に少々変更
"""

class Logger():
    """ログ出力用クラス
    """
    @classmethod
    def init(cls, filename:str=None, force:bool=True) -> None:
        logging.basicConfig(force=force, filename=filename, level=logging.DEBUG, format="[%(asctime)s] [%(process)d] [%(name)s] [%(levelname)s] %(message)s")

    @classmethod
    def debug(cls, execution_location, log_message):
        _lggr = logging.getLogger(execution_location)
        _lggr.debug(log_message)

    @classmethod
    def info(cls, execution_location, log_message):
        _lggr = logging.getLogger(execution_location)
        _lggr.info(log_message)

    @classmethod
    def warning(cls, execution_location, log_message):
        _lggr = logging.getLogger(execution_location)
        _lggr.warning(log_message)

    @classmethod
    def error(cls, execution_location, log_message):
        _lggr = logging.getLogger(execution_location)
        _lggr.error(log_message)

class Trace():
    """ログ出力とセットで使う処理をまとめたクラス
    """
    @classmethod
    def execution_location(self):
        """
        処理の実行場所を出力する。[ファイル名: 行番号]
        """
        frame = inspect.currentframe().f_back
        # 処理の実行場所を出力する。[ファイル名: 行番号]
        return "{}:{}".format(os.path.basename(frame.f_code.co_filename), frame.f_lineno)
        # # 処理の実行場所を出力する。[ファイル名: 行番号  メソッド名]
        # return "{}:{} {}".format(os.path.basename(frame.f_code.co_filename), frame.f_lineno, frame.f_code.co_name)

def test_method():
    Logger.init()
    Logger.debug(Trace.execution_location(), 'console debug test')
    Logger.init(filename=r'__hogehoge.log')
    Logger.debug(Trace.execution_location(), 'file debug test')

if __name__ == '__main__':
    test_method()
    pass